namespace SKYCOM.DLManagement.AzureHelper
{
    public class Constants
    {
        public struct BlobConstants
        {
            public const string ConnectionString = "blobconnectionstring";
            public const string StorageAccountName = "blobaccountname";
            public const string ContainerName = "ContainerName";
            public const string ConnectionFailedErrorMessage = "Blob connection failed using managed identity. Exception in detail : ";
            public const string StorageNameKeyNotfoundErrorMessage = "StorageAccountName not found in azure keyvault";
            public const string ContainerNameKeyNotyfoundErrorMessage = "ContainerName not found in azure keyvault";
        }
    }
}