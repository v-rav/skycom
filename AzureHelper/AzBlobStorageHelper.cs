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
    using System;
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

        private BlobContainerClient GetBlobContainerClientUsingManagedIdentity(string containerName)
        {
            try
            {
                // Managed Identity Blob Service URI              
                if (string.IsNullOrEmpty(storageAccountName))
                {
                    throw new KeyNotFoundException(Constants.BlobConstants.StorageNameKeyNotfoundErrorMessage);
                }
                string blobServiceUri = $"https://{storageAccountName}.blob.core.windows.net";
                var credential = new DefaultAzureCredential();

                BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobServiceUri), credential);
                if (string.IsNullOrEmpty(containerName))
                {
                    throw new KeyNotFoundException(Constants.BlobConstants.ContainerNameKeyNotyfoundErrorMessage);
                }
                return blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch
            {

                throw;
            }
        }

        /// <summary>
        /// Access blob with sas token to access the blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        private BlobClient AccessBlobWithSasTocken(string blobName)
        {
            //Client has to decide which container name
            var containerName = settings.Value.BlobSettings.CommonContainerName;
            //Second option
            var blobServiceClient = new BlobServiceClient(settings.Value.BlobSettings.ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            // Specify the expiration time of the SAS token
            DateTimeOffset expirationTime = DateTimeOffset.UtcNow.AddHours(1); // token expires in 1 hour

            // Create a SAS token with read permissions (you can customize this)
            var sasToken = blobClient.GenerateSasUri(
                 BlobSasPermissions.All, // Required Permissions, // Permissions: Read
                expirationTime // Expiration time
            );

            // Create a new BlobClient using the SAS URI
            var sasBlobClient = new BlobClient(sasToken);


            // Return the blob url with SAS token 
            return sasBlobClient;
        }

        /// <summary>
        /// Access blob with sas token to access the blob
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        private BlobContainerClient AccessBlobContainerClientWithSasTocken(string containerName)
        {
            var blobServiceClient = new BlobServiceClient(settings.Value.BlobSettings.ConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            // Specify the expiration time of the SAS token
            DateTimeOffset expirationTime = DateTimeOffset.UtcNow.AddHours(1); // token expires in 1 hour

            // Create a SAS token with read permissions (you can customize this)
            var sasToken = blobContainerClient.GenerateSasUri(
                BlobContainerSasPermissions.All, // Permissions: Read
                expirationTime // Expiration time
            );

            // Create a new BlobClient using the SAS URI
            var sasBlobContainerClient = new BlobContainerClient(sasToken);

            // Return the blob url with SAS token 
            return sasBlobContainerClient;
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
                //Getting the blob client from azure blob storage using managed Identity
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);

                // Create the container if it doesn't exist
                containerClient.CreateIfNotExists();

                // Create a blob client for the file
                //BlobClient blobClient = containerClient.GetBlobClient(fileName);

                //Local testing - use sas access token
                var blobClient = AccessBlobWithSasTocken(fileName);

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
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
                if (!containerClient.Exists())
                {
                    throw new Exception($"AzBlobStorageHelper : DownloadblobAsync - Container not exists  {containerName}");
                }

                // Get a reference to the blob (file)
                //BlobClient blobClient = containerClient.GetBlobClient(blobName);

                //Local testing
                var blobClient = AccessBlobWithSasTocken(blobName);
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

                #region CMF - Local Debugging
                //Below lines will be uncommented for local testing
                //catch (RequestFailedException ex) when (ex.Status == 403)
                //{
                //     return AccessBlobWithSasTocken(blobName); 
                //}
                #endregion
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

                //BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
                //var blobClient = containerClient.GetBlobClient(blobName); // Return the BlobClient for the specified blob           
                //Local testing
                var blobClient = AccessBlobWithSasTocken(blobName);
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

        public List<BlobFileInfo> GetBlobList(string containerName = "")
        {
            var blobsList = new List<BlobFileInfo>();

            // Managed Identity Blob Service URI              
            if (string.IsNullOrEmpty(storageAccountName))
            {
                throw new KeyNotFoundException(Constants.BlobConstants.StorageNameKeyNotfoundErrorMessage);
            }
            string blobServiceUri = $"https://{storageAccountName}.blob.core.windows.net";
            var credential = new DefaultAzureCredential();

            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobServiceUri), credential);

            foreach (BlobContainerItem containerItem in blobServiceClient.GetBlobContainers())
            {
                // Create a new BlobClient using the SAS URI
                var sasBlobContainerClient = AccessBlobContainerClientWithSasTocken(containerItem.Name);              
                var blobs = sasBlobContainerClient.GetBlobs();
                foreach (var blobItem in blobs)
                {
                    blobsList.Add(new BlobFileInfo { ContainerName = containerItem.Name,
                                                     FileName= blobItem.Name,
                                                     FileSize = blobItem.Properties.ContentLength.Value,
                                                     Date = blobItem.Properties.LastModified.Value.UtcDateTime});
                }
            }

            return blobsList;
        }
        #endregion
    }
}