#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NodaTime;
using TidyData.SnapshotLog;
using TidyUtility.Core;
using TidyUtility.Data.Json;

namespace TidyData.Azure.SnapshotLog
{
    // Basic Docs:
    //     https://github.com/Azure/azure-sdk-for-net/blob/Azure.Storage.Blobs_12.6.0/sdk/storage/Azure.Storage.Blobs/README.md
    // Error Codes from RequestFailedException.ErrorCode:
    //     https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes

    public class AzureBlockBlobSnapshotLog<T> : AbstractSnapshotLog<T>
        where T : new()
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly BlobContainerClient _container;
        private bool _containerKnownToExist;
        private readonly string _snapshotPath;

        public AzureBlockBlobSnapshotLog(SnapshotLogSettings snapshotLogSettings, 
            string storageConnectionString, string blobContainerName, string snapshotPath, 
            ISerializer serializer = null, IClock clock = null)
            : base(snapshotLogSettings, serializer, clock)
        {
            _serviceClient = new BlobServiceClient(storageConnectionString);
            _container = _serviceClient.GetBlobContainerClient(blobContainerName);
            _snapshotPath = snapshotPath;
        }

        protected override async Task<string> SaveNewSnapshotAsync(T instanceToSave)
        {
            await EnsureContainerExists();

            // NEW:

            string snapshotName = this.BuildSnapshotName();
            string blockBlobName = BuildBlockBlobName(snapshotName);
            string serializedData = this.Serializer.Serialize(instanceToSave);

            BlobClient blobClient = _container.GetBlobClient($"{_snapshotPath}/{blockBlobName}");
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData)))
            {
                await blobClient.UploadAsync(memStream);
            }

            // OLD:

            //CloudBlobDirectory directory = this._container.GetDirectoryReference(this._snapshotPath);
            //string snapshotName = this.BuildSnapshotName();
            //CloudBlockBlob blockBlob = directory.GetBlockBlobReference(this.BuildBlockBlobName(snapshotName));
            
            //string serializedData = this.Serializer.Serialize(instanceToSave);
            //await blockBlob.UploadTextAsync(serializedData);

            return snapshotName;
        }

        public override async Task<T> LoadSnapshotAsync(string snapshotName)
        {
            await EnsureContainerExists();

            // NEW:

            string blockBlobName = BuildBlockBlobName(snapshotName);

            BlobClient blobClient = _container.GetBlobClient($"{_snapshotPath}/{blockBlobName}");

            string blobAsText;
            try
            {
                using (var memStream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(memStream);
                    blobAsText = Encoding.UTF8.GetString(memStream.ToArray());
                }
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound && exc.ErrorCode == "BlobNotFound")
            {
                throw new SnapshotNotFoundException($"Unable to read block blob at path \"{_snapshotPath}\" named \"{snapshotName}\".", exc);
            }

            // OLD:

            //CloudBlobDirectory directory = this._container.GetDirectoryReference(this._snapshotPath);
            //CloudBlockBlob blockBlob = directory.GetBlockBlobReference(this.BuildBlockBlobName(snapshotName));

            //string blobAsText;
            //try
            //{
            //    blobAsText = await blockBlob.DownloadTextAsync(null, null, null, null);
            //}
            //catch (StorageException exc)
            //{
            //    throw new SnapshotNotFoundException($"Unable to read block blob at path \"{this._snapshotPath}\" named \"{snapshotName}\".", exc);
            //}

            return string.IsNullOrWhiteSpace(blobAsText)
                ? Factory<T>.Create()
                : this.Serializer.Deserialize<T>(blobAsText);
        }

        public override async Task DeleteAsync(string snapshotName)
        {
            await EnsureContainerExists();
            await DeleteBlockBlobAsync(BuildBlockBlobName(snapshotName));
        }

        public override async Task DeleteAllAsync()
        {
            await EnsureContainerExists();

            IEnumerable<string> savedFileNames = await GetSavedBlockBlobsAsync();
            foreach (string fileName in savedFileNames)
                await DeleteBlockBlobAsync(fileName);
        }

        public override async Task DeleteAllEligibleForAutoDeletionAsync()
        {
            await EnsureContainerExists();

            IEnumerable<string> snapshotsEligibleForDeletion = await this.GetAllSnapshotNamesEligibleForAutoDeletionAsync();

            foreach (string snapshotName in snapshotsEligibleForDeletion)
                await DeleteBlockBlobAsync(BuildBlockBlobName(snapshotName));
        }

        public override async Task<IEnumerable<string>> GetSavedSnapshotNamesAsync()
        {
            await EnsureContainerExists();

            return (await GetSavedBlockBlobsAsync())
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();

        }

        private async Task<IEnumerable<string>> GetSavedBlockBlobsAsync()
        {
            // NEW:

            IEnumerable<string> fullPaths = Enumerable.Empty<string>();
            AsyncPageable<BlobItem> blobsAsync = _container.GetBlobsAsync(prefix: _snapshotPath);
            IAsyncEnumerator<BlobItem> blobEnumerator = blobsAsync.GetAsyncEnumerator();
            
            try
            {
                while (await blobEnumerator.MoveNextAsync())
                    fullPaths = fullPaths.Append(blobEnumerator.Current.Name);
            }
            finally
            {
                await blobEnumerator.DisposeAsync();
            }

            IEnumerable<string> result = fullPaths
                .Select(x => x.Split(new char[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries).Last())
                .Where(s => this.IsSnapshotNameMatch(Path.GetFileNameWithoutExtension(s)));
            


            // OLD:

            //CloudBlobDirectory directory = this._container.GetDirectoryReference(this._snapshotPath);

            //// Should be far less than 500, but in case we get crazy with the rules, better loop...
            //IEnumerable<string> result = Enumerable.Empty<string>();
            //BlobContinuationToken token = null;
            //do
            //{
            //    BlobResultSegment segment = await directory.ListBlobsSegmentedAsync(token);
            //    result = result.Concat(segment.Results
            //        .Select(s => s.Uri.Segments.Last())
            //        .Where(s => this.IsSnapshotNameMatch(Path.GetFileNameWithoutExtension(s))));
            //    token = segment.ContinuationToken;
            //} while (token != null);

            return result;
        }

        private async Task DeleteBlockBlobAsync(string fileName)
        {
            // NEW:

            BlobClient blobClient = _container.GetBlobClient($"{_snapshotPath}/{fileName}");
            await blobClient.DeleteIfExistsAsync();
            

            // OLD:

            //CloudBlobDirectory directory = this._container.GetDirectoryReference(this._snapshotPath);
            //CloudBlockBlob blockBlob = directory.GetBlockBlobReference(fileName);
            //await blockBlob.DeleteIfExistsAsync();
        }

        private IEnumerable<string> GetBlockBlobMatchingSnapshotPattern(IEnumerable<string> fileNames)
        {
            return fileNames
                .Select(fileName => new
                {
                    FileName = fileName,
                    SnapshotName = Path.GetFileNameWithoutExtension(fileName),
                })
                .Where(x => this.IsSnapshotNameMatch(x.SnapshotName))
                .Select(x => x.FileName);
        }

        private string BuildBlockBlobName(string snapshotName)
        {
            return $"{snapshotName}{this.Settings.FileExtension}";
        }

        private string BuildBlockBlobName()
        {
            return this.BuildBlockBlobName(this.BuildSnapshotName());
        }

        private async Task EnsureContainerExists()
        {
            if (!_containerKnownToExist)
            {
                //NEW:
                await _container.CreateIfNotExistsAsync();

                // OLD:
                //await this._container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null);
                
                _containerKnownToExist = true;
            }
        }
    }
}
