namespace SKYCOM.DLManagement.AzureHelper
{
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Sas;
    using Humanizer.Configuration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using NuGet.Protocol.Core.Types;
    using Org.BouncyCastle.Utilities.IO;
    using SKYCOM.DLManagement.Data;
    using SKYCOM.DLManagement.Util;
    using System;
    using System.ClientModel;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// This is created by - CMF for Appservice migration
    /// </summary>
    public class AzBlobStorageHelper
    {
        // private readonly BlobSettings blobSettings;
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
                    return GetBlobContainerClientUsingManagedIdentity(containerName);
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

        //private BlobClient GetBlobClient(string containerName, string blobName)
        //{
        //    // Retrieve connection string from settings (if available)
        //    if (!string.IsNullOrEmpty(settings.Value.BlobSettings.ConnectionString))
        //    {              
        //        // If no connection string, use Managed Identity for authentication
        //        return AccessBlobWithSasTocken(blobName, containerName);
        //    }
        //    else
        //    {
        //        BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
        //        return containerClient.GetBlobClient(blobName);
        //    }
        //}

        private BlobContainerClient GetBlobContainerClientUsingManagedIdentity(string containerName)
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
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="containerName"></param>
        /// <exception cref="Exception"></exception>
        public void UploadFileToAzure(Stream memoryStream, string fileName, string containerName)
        {
            try
            {
                ////Getting the blob client from azure blob storage using managed Identity
                BlobContainerClient containerClient = GetBlobContainerClient(containerName);

                var blobClient = containerClient.GetBlobClient(fileName);
                // Upload the file stream to Azure Blob Storage
                using (memoryStream)
                {
                    // Reset the memory stream position to the beginning before using it
                    memoryStream.Position = 0;
                    blobClient.UploadAsync(memoryStream, overwrite: true);
                }

            }
            catch (Exception ex) { throw new Exception("Blobstorage Helper - UploadFileToAzure Method : Some error while uploading the blob - ", ex); }
        }

        /// <summary>
        /// Download blob content - using managed identity using memorystream.
        /// </summary>
        public MemoryStream DownloadBlobToMemoryStream(string containerName, string blobName)
        {
            try
            {
                //Client has to replace the container name with actual one.
                if (string.IsNullOrEmpty(containerName))
                {
                    containerName = settings.Value.BlobSettings.CommonContainerName;
                }
                // Get a reference to the container
                BlobContainerClient containerClient = GetBlobContainerClient(containerName);
                if (containerClient == null)
                {
                    throw new Exception($"AzBlobStorageHelper : DownloadblobAsync - Container not exists  {containerName}");
                }

                // Get a reference to the blob (file)
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                if (blobClient.Exists())
                {
                    // Create a MemoryStream to hold the downloaded content
                    MemoryStream memoryStream = new MemoryStream();

                    // Download the blob content into the memory stream
                    blobClient.DownloadTo(memoryStream);

                    // Reset the memory stream position to the beginning before using it
                    memoryStream.Position = 0;

                    return memoryStream;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }

        public string DownloadBlobContent(string containerName, string blobName)
        {
            try
            {

                BlobContainerClient containerClient = GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName); // Return the BlobClient for the specified blob   
                if (blobClient.Exists())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        blobClient.DownloadTo(memoryStream); // Download the blob to the memory stream
                        memoryStream.Position = 0; // Reset the position to read from the beginning
                        using (var reader = new StreamReader(memoryStream))
                        {
                            return reader.ReadToEnd(); // Return the content as a string
                        }
                    }
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }

        public async Task<List<ServerFileInfo>> GetBlobList_old(string containerName)
        {
            var blobsList = new List<ServerFileInfo>();

            //For the first time, load all the containers of the storageaccount, so assigning the containername to storage account.
            if (string.IsNullOrEmpty(containerName))
            {
                containerName = storageAccountName;
            }

            // Managed Identity Blob Service URI              
            if (string.IsNullOrEmpty(storageAccountName))
            {
                throw new KeyNotFoundException(Constants.BlobConstants.StorageNameKeyNotfoundErrorMessage);
            }
            string blobServiceUri = $"https://{storageAccountName}.blob.core.windows.net";
            var credential = new DefaultAzureCredential();

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobServiceUri), credential);
            if (containerName == storageAccountName) //List all containers if the full path is root directory
            {
                foreach (BlobContainerItem containerItem in blobServiceClient.GetBlobContainers())
                {
                    blobsList.Add(new ServerFileInfo() { FileName = containerItem.Name, IsFile = false, FullPath = containerItem.Name, Attributes = FileAttributes.Directory });
                }
                return blobsList;
            }
            else //List the folders and files of the current container/directory
            {
                // Create a new BlobClient using the SAS URI
                var blobContainerClient = GetBlobContainerClient(containerName);
                // List all blobs (files and folders) in the container
                await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
                {
                    // Check if the blob is a directory (virtual folder) or file
                    bool isFile = !blobItem.Name.EndsWith("/"); // Treat "folders" as blobs with a trailing "/"

                    blobsList.Add(new ServerFileInfo
                    {

                        FileName = blobItem.Name,
                        FileSize = (int)blobItem.Properties.ContentLength,
                        Date = blobItem.Properties.LastModified.Value.UtcDateTime,
                        IsFile = isFile,
                        Attributes = FileAttributes.Normal,
                        FullPath = containerName
                    });
                }
            }

            return blobsList;
        }

        public List<string> GetBlobContainers()
        {
            List<string> containers = new List<string>();
            // Managed Identity Blob Service URI              
            if (string.IsNullOrEmpty(storageAccountName))
            {
                throw new KeyNotFoundException(Constants.BlobConstants.StorageNameKeyNotfoundErrorMessage);
            }
            string blobServiceUri = $"https://{storageAccountName}.blob.core.windows.net";
            var credential = new DefaultAzureCredential();

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobServiceUri), credential);
            containers.AddRange(blobServiceClient.GetBlobContainers().ToList().Select(x => x.Name));
            return containers;
        }

        public async Task<List<ServerFileInfo>> GetBlobList(string containerName, string continuationToken, int pageSize)
        {
            var blobsList = new List<ServerFileInfo>();
            var blobContainerClient = GetBlobContainerClient(containerName);
            var pages = blobContainerClient.GetBlobsAsync().AsPages(continuationToken, pageSize);

            await foreach (var page in blobContainerClient.GetBlobsAsync().AsPages(continuationToken, pageSize))
            {
                continuationToken = page.ContinuationToken; // Store continuation token for next page requests

                blobsList.AddRange(page.Values.Select(blobItem => new ServerFileInfo
                {
                    FileName = blobItem.Name,
                    FileSize = (int)(blobItem.Properties.ContentLength ?? 0), // Handle null case
                    Date = blobItem.Properties.LastModified?.UtcDateTime ?? DateTime.MinValue, // Handle null case
                    IsFile = true, // Assuming it's a file
                    Attributes = FileAttributes.Normal,
                    FullPath = containerName
                }));                
            }
            return blobsList;
        }



        #endregion
    }
}