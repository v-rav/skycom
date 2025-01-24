namespace SKYCOM.DLManagement.AzureHelper
{
    public class Constants
    {
        public struct BlobConstants
        {         
            public const string ConnectionFailedErrorMessage = "Blob connection failed. Exception in detail : ";
            public const string StorageNameKeyNotfoundErrorMessage = "StorageAccountName Required";
            public const string ContainerNameKeyNotyfoundErrorMessage = "ContainerName Required";
        }
    }
}