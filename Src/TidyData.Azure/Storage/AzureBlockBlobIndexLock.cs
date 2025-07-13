 #nullable disable
 using System.Net;
 using System.Text;
 using Azure;
 using Azure.Storage.Blobs;
 using Azure.Storage.Blobs.Models;
 using NodaTime;
 using TidyData.Storage;
 using TidyUtility.Data.Json;

 namespace TidyData.Azure.Storage
{
    // See:
    //     https://docs.microsoft.com/en-us/azure/storage/blobs/concurrency-manage?tabs=dotnet

    public class AzureBlockBlobIndexLock : IIndexLock
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _container;
        private readonly string _blobFilePathName;
        private readonly ISerializer _serializer;
        private readonly IClock _clock;
        private ETag? _eTag;
        private bool _containerKnownToExist;

        public AzureBlockBlobIndexLock(string storageConnectionString, string blobContainerName, string blobFilePathName, 
            ISerializer serializer, IClock clock)
        {
            this._serviceClient = new BlobServiceClient(storageConnectionString);
            this._container = this._serviceClient.GetBlobContainerClient(blobContainerName);
            this._blobFilePathName = blobFilePathName;
            this._serializer = serializer;
            this._clock = clock;
        }

        // For Testing
        internal async Task DeleteAsync()
        {
            await this.EnsureContainerExists();
            
            BlobClient blobClient = this._container.GetBlobClient(this._blobFilePathName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<DBStorageIndex> ReadAndLockAsync()
        {
            await this.EnsureContainerExists();

            BlobClient blobClient = this._container.GetBlobClient(this._blobFilePathName);
            string blobAsText = null;
            try
            {
                using (var memStream = new MemoryStream())
                {
                    Response<BlobProperties> propertiesResponse = await blobClient.GetPropertiesAsync();
                    this._eTag = propertiesResponse.Value.ETag;

                    Response response = await blobClient.DownloadToAsync(memStream,
                        conditions: new BlobRequestConditions() {IfMatch = this._eTag});
                    blobAsText = Encoding.UTF8.GetString(memStream.ToArray());
                }
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.PreconditionFailed && exc.ErrorCode == "ConditionNotMet")
            {
                throw new StorageConcurrencyException(exc);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound && exc.ErrorCode == "BlobNotFound")
            {
                // When creating a blob, this is as intended. While expected, this is a temporary
                // situation for this blob file and should be the most efficient implementation as it only
                // involves one round trip to the server to get the file or determine it doesn't exist'.
                // Exception handling, while inefficient, is only needed until the file exists on the first call.

                // Create the blob to create the ETag value.
                string serializedData = this._serializer.Serialize(new DBStorageIndex());
                using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData)))
                {
                    Response<BlobContentInfo> contentInfoResponse = await blobClient.UploadAsync(memStream);
                    this._eTag = contentInfoResponse.Value.ETag;
                }

                blobAsText = serializedData;
            }

            return string.IsNullOrWhiteSpace(blobAsText)
                ? new DBStorageIndex()
                : this._serializer.Deserialize<DBStorageIndex>(blobAsText);
        }

        public async Task UpdateAndReleaseLockAsync(DBStorageIndex dbStorageIndex)
        {
            await this.EnsureContainerExists();

            if (this._eTag == null)
                throw new InvalidOperationException($"Must call the {nameof(this.ReadAndLockAsync)}() method " +
                    $"before calling {nameof(this.UpdateAndReleaseLockAsync)}().");

            string blobAsText = this._serializer.Serialize(dbStorageIndex);
            
            try
            {
                BlobClient blobClient = this._container.GetBlobClient(this._blobFilePathName);
                
                using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(blobAsText)))
                {
                    Response<BlobContentInfo> contentInfoResponse = await blobClient.UploadAsync(memStream,
                        conditions: new BlobRequestConditions() { IfMatch = this._eTag });
                }
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.PreconditionFailed && exc.ErrorCode == "ConditionNotMet")
            {
                throw new StorageConcurrencyException(exc);
            }
        }

        public Task ReleaseLockAsync()
        {
            this._eTag = null;
            return Task.CompletedTask;
        }

        private async Task EnsureContainerExists()
        {
            if (!this._containerKnownToExist)
            {
                await this._container.CreateIfNotExistsAsync();
                this._containerKnownToExist = true;
            }
        }
    }
}