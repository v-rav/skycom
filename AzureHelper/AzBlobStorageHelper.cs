namespace SKYCOM.DLManagement.AzureHelper
{
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Azure.Storage.Sas;
    using NuGet.Protocol.Core.Types;
    using Org.BouncyCastle.Utilities.IO;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;

    /// <summary>
    /// This is created by - CMF for Appservice migration
    /// </summary>
    public class AzBlobStorageHelper
    {
        #region private methods

        private static BlobContainerClient GetBlobContainerClientUsingManagedIdentity(string containerName)
        {
            try
            {
                // Managed Identity Blob Service URI
                string storageAccName = ConfigurationManager.AppSettings[Constants.BlobConstants.StorageAccountName];
                if (string.IsNullOrEmpty(storageAccName))
                {
                    throw new KeyNotFoundException(Constants.BlobConstants.StorageNameKeyNotfoundErrorMessage);
                }
                string blobServiceUri = $"https://{storageAccName}.blob.core.windows.net";
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
        private static string AccessBlobWithSasTocken(string blobName)
        {
            //Second option
            var blobServiceClient = new BlobServiceClient(ConfigurationManager.AppSettings[Constants.BlobConstants.ConnectionString]);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(ConfigurationManager.AppSettings[Constants.BlobConstants.ContainerName]);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            // Specify the expiration time of the SAS token
            DateTimeOffset expirationTime = DateTimeOffset.UtcNow.AddHours(1); // token expires in 1 hour

            // Create a SAS token with read permissions (you can customize this)
            var sasToken = blobClient.GenerateSasUri(
                BlobSasPermissions.Read, // Permissions: Read
                expirationTime // Expiration time
            ).Query;

            // Return the blob url with SAS token 
            return $"https://{ConfigurationManager.AppSettings[Constants.BlobConstants.StorageAccountName]}.blob.core.windows.net/{Constants.BlobConstants.ContainerName}/{blobName}{sasToken}";
        }

        #endregion

        #region public methods
        /// <summary>
        /// Download blob content - using managed identity and if its not accessible, get the sas token url to access the same.
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public static Stream DownloadBlobAsync(string blobName, string containerName)
        {
            try
            {
                //Getting the blob url from azure blob storage using managed Identity
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
                if(!containerClient.Exists())
                {
                    throw new Exception($"AzBlobStorageHelper : DownloadblobAsync - Container not exists  { containerName}");
                }
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                               
                
                // download blob content into filestream
                // Download the blob to a file
                using (MemoryStream ms = new MemoryStream())
                {
                    blobClient.DownloadTo(ms);
                    return ms;
                }
                
                #region CMF - Local Debugging
                //return AccessBlobWithSasTocken(blobName); 
                #endregion

            }
            #region CMF - Local Debugging
            //Below lines will be uncommented for local testing
            //catch (RequestFailedException ex) when (ex.Status == 403)
            //{
            //     return AccessBlobWithSasTocken(blobName); 
            //}
            #endregion
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }
        #endregion

        #region
        /// <summary>
        /// Get Blob URL using managed identity and if its not accessible, get the sas token url to access the same.
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        public static string GetBlobURL(string blobName, string containerName)
        {
            try
            {
                //Getting the blob url from azure blob storage using managed Identity
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);
                return blobClient.Uri.ToString();  //This will be commented for local testing

                #region CMF - Local Debugging
                //return AccessBlobWithSasTocken(blobName); 
                #endregion

            }
            #region CMF - Local Debugging
            //Below lines will be uncommented for local testing
            //catch (RequestFailedException ex) when (ex.Status == 403)
            //{
            //     return AccessBlobWithSasTocken(blobName); 
            //}
            #endregion
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }       

        /// <summary>
        /// Get blob urls for list of blobnames
        /// </summary>
        /// <param name="blobNames"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Dictionary<string, string> GetBlobURLs(List<string> blobNames, string containerName)
        {
            var blobUrlMap = new Dictionary<string, string>();

            try
            {

                // Managed Identity Blob Service URI                
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);

                // Iterate through the array of blob names and get URLs
                foreach (string blobName in blobNames)
                {
                    var blobClient = containerClient.GetBlobClient(blobName);
                    blobUrlMap[blobName] = blobClient.Uri.ToString();                    
                }

                #region CMF - Local Debugging
                //// Generate SAS tokens for all blobs in case of access failure - will remove this once managed identity works
                //foreach (string blobName in blobNames)
                //{                  
                //    blobUrlMap[blobName] = AccessBlobWithSasTocken(blobName); ;
                //}
                #endregion
            }
            #region CMF - Local Debugging
            //catch (RequestFailedException ex) when (ex.Status == 403)
            //{
            //    // Generate SAS tokens for all blobs in case of access failure
            //    foreach (string blobName in blobNames)
            //    {
            //        blobUrlMap[blobName] = AccessBlobWithSasTocken(blobName);
            //    }
            //}
            #endregion
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }

            return blobUrlMap;
        }

        /// <summary>
        /// Upload file into blob container
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="containerName"></param>
        /// <exception cref="Exception"></exception>
        public static void UploadFileToAzure(Stream memoryStream, string fileName, string containerName)
        {
            try
            {               
                //Getting the blob client from azure blob storage using managed Identity
                BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);

                // Create the container if it doesn't exist
                containerClient.CreateIfNotExists();

                // Create a blob client for the file
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Upload the file stream to Azure Blob Storage
                using (memoryStream)
                {
                    blobClient.Upload(memoryStream, overwrite: true);
                }

            }
            catch (Exception ex) { throw new Exception("Blobstorage Helper - UploadFileToAzure Method : Some error while uploading the blob - ", ex); }
        }

        /// <summary>
        /// Download blob content - using managed identity using memorystream.
        /// </summary>
        public static MemoryStream DownloadBlobToMemoryStream(string containerName, string blobName)
        {
            try {

            // Get a reference to the container
             BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);

            // Get a reference to the blob (file)
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Create a MemoryStream to hold the downloaded content
            MemoryStream memoryStream = new MemoryStream();

            // Download the blob content into the memory stream
            blobClient.DownloadTo(memoryStream);

            // Reset the memory stream position to the beginning before using it
            memoryStream.Position = 0;

            return memoryStream;
        }
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }

        /// <summary>
        /// Gets blobclient of blobname.
        /// </summary>
        public static BlobClient GetBlobClient(string containerName, string blobName)
        {
            BlobContainerClient containerClient = GetBlobContainerClientUsingManagedIdentity(containerName);
            return containerClient.GetBlobClient(blobName); // Return the BlobClient for the specified blob
        }

        /// <summary>
        /// checks if blob exists or not.
        /// </summary>
        public static bool BlobExists(BlobClient blobClient)
        {
            return blobClient.Exists(); // Check if the blob exists synchronously
        }

        public static string DownloadBlobContent(BlobClient blobClient)
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception(string.Concat(Constants.BlobConstants.ConnectionFailedErrorMessage, ex.Message));
            }
        }
        #endregion
    }
}