namespace SKYCOM.DLManagement.AzureHelper
{
    using Azure;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Azure.Storage.Sas;
    using Humanizer.Configuration;
    using Microsoft.CodeAnalysis.Elfie.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using NuGet.Protocol.Core.Types;
    using Org.BouncyCastle.Utilities.IO;
    using SKYCOM.DLManagement.Data;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;

    public class AzBlobStorageHelper
    {
        private readonly string storageAccountName;
        private readonly IOptions<Settings> settings;

        public AzBlobStorageHelper(IConfiguration configuration, IOptions<Settings> settings)
        {
            this.settings = settings;
            storageAccountName = settings.Value.BlobSettings.StorageAccountName;
        }

        #region private methods

        /// <summary>
        /// Gets a BlobContainerClient using either Managed Identity or a connection string.
        /// </summary>
        /// <param name="containerName">The name of the container to access.</param>
        /// <returns>BlobContainerClient to interact with the container.</returns>
        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            try
            {
                // Retrieve connection string from settings (if available)
                string connectionString = settings.Value.BlobSettings.ConnectionString;
                if (!string.IsNullOrEmpty(connectionString))
                {
                    // If connection string is provided, use it for authentication
                    return GetBlobContainerClientUsingConnectionString(containerName, connectionString);
                }
                else
                {
                    // If no connection string, use Managed Identity for authentication
                    string storageAccountName = settings.Value.BlobSettings.StorageAccountName;
                    return GetBlobContainerClientUsingManagedIdentity(containerName, storageAccountName);
                }
            }
            catch (Exception ex)
            {
                // Log the exception or rethrow as necessary
                Console.WriteLine($"Error while getting BlobContainerClient: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a BlobContainerClient using a connection string.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="connectionString">The connection string to use for authentication.</param>
        /// <returns>BlobContainerClient</returns>
        private BlobContainerClient GetBlobContainerClientUsingConnectionString(string containerName, string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(containerName))
                {
                    throw new KeyNotFoundException("Container name is required.");
                }

                var blobServiceClient = new BlobServiceClient(connectionString);
                return blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using connection string for BlobContainerClient: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a BlobContainerClient using Managed Identity.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="storageAccountName">The name of the storage account.</param>
        /// <returns>BlobContainerClient</returns>
        private BlobContainerClient GetBlobContainerClientUsingManagedIdentity(string containerName, string storageAccountName)
        {
            try
            {
                if (string.IsNullOrEmpty(storageAccountName))
                {
                    throw new KeyNotFoundException("Storage account name is required.");
                }

                string blobServiceUri = $"https://{storageAccountName}.blob.core.windows.net";
                var credential = new DefaultAzureCredential();

                var blobServiceClient = new BlobServiceClient(new Uri(blobServiceUri), credential);
                if (string.IsNullOrEmpty(containerName))
                {
                    throw new KeyNotFoundException("Container name is required.");
                }

                return blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error using Managed Identity for BlobContainerClient: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Upload file into blob container
        /// </summary>
        /// <param name="memoryStream">Memory stream containing the file data.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <exception cref="Exception">Throws exception if file upload fails.</exception>
        public void UploadFileToAzure(Stream memoryStream, string fileName, string containerName)
        {
            try
            {
                // Retrieve connection string or managed identity BlobContainerClient
                BlobContainerClient containerClient = GetBlobContainerClient(containerName);

                // Create the container if it doesn't exist
                containerClient.CreateIfNotExists();

                // Create a blob client for the file
                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                // Ensure the memoryStream position is at the start before uploading
                    memoryStream.Position = 0;
                // Upload the file stream to Azure Blob Storage
                using (memoryStream)
                {
                    blobClient.Upload(memoryStream, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Blobstorage Helper - UploadFileToAzure Method: Some error while uploading the blob", ex);
            }
        }

        /// <summary>
        /// Download blob content using either managed identity or connection string into a memory stream.
        /// </summary>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="blobName">The name of the blob (file).</param>
        /// <returns>A memory stream containing the blob content.</returns>
        public MemoryStream DownloadBlobToMemoryStream(string containerName, string blobName)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                containerName = string.IsNullOrEmpty(containerName) ? "skycomcontainer-1" : containerName;

                // Get BlobContainerClient using connection string or managed identity
                BlobContainerClient containerClient = GetBlobContainerClient(containerName);
               
                if (!containerClient.Exists())
                {
                    throw new Exception($"AzBlobStorageHelper : DownloadblobAsync - Container not found {containerName}");
                }

                // Get a reference to the blob (file)
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if (blobClient.Exists())
                {
                    // Create a MemoryStream to hold the downloaded content
                   // MemoryStream memoryStream = new MemoryStream();

                    // Download the blob content into the memory stream
                    blobClient.DownloadTo(memoryStream);

                    // Reset the memory stream position to the beginning before using it
                    memoryStream.Position = 0;

                    return memoryStream;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
               throw new Exception($"AzBlobStorageHelper : DownloadBlobToMemoryStream Method: {ex.Message}", ex);
            }
            finally {                    

            }
           
        }
        public string DownloadBlobContent(string containerName, string blobName)
        {
            try
            {
                // If containerName is empty or null, fallback to default container from settings
                if (string.IsNullOrEmpty(containerName))
                {
                    containerName = settings.Value.BlobSettings.CommonContainerName;
                }

                // Get BlobContainerClient using managed identity
                BlobContainerClient containerClient = GetBlobContainerClient(containerName);

                // Get the BlobClient for the specified blob
                var blobClient = containerClient.GetBlobClient(blobName);

                // Check if the blob exists
                if (blobClient.Exists())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        // Download the blob content to the memory stream
                        blobClient.DownloadTo(memoryStream);

                        // Reset memory stream position to read from the beginning
                        memoryStream.Position = 0;

                        // Use StreamReader to read the content as a string
                        using (var reader = new StreamReader(memoryStream))
                        {
                            return reader.ReadToEnd(); // Return the blob content as a string
                        }
                    }
                }
                else
                {
                    // Return null if the blob doesn't exist
                    return null;
                }
            }
            catch (RequestFailedException ex)
            {
                // Specific error handling for Azure SDK failures
                throw new Exception($"Azure request failed. Blob not found or access issues: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // General exception handling for other errors
                throw new Exception($"An error occurred while downloading the blob: {ex.Message}", ex);
            }
        }


        #endregion
    }
}
