 #nullable disable
using NodaTime;
using TidySyncDB.Storage;
using TidyUtility.Serializer;
using TidyUtility.Storage;

namespace TidySyncDB.Azure.Storage
{
    public sealed class AzureBlockBlobDBStorage<T> : DBStorageBase<T>
        where T : class, new()
    {
        public AzureBlockBlobDBStorage(SnapshotLogSettings snapshotLogSettings, 
            string storageConnectionString, string blobContainerName, string snapshotPath,
            ISerializer serializer, IClock clock = null)
        {
            snapshotPath = snapshotPath.Replace('\\', '/').TrimEnd('/');

            this.Clock = clock ?? SystemClock.Instance;
            this.SnapshotLog = new AzureBlockBlobSnapshotLog<T>(snapshotLogSettings, 
                storageConnectionString, blobContainerName, snapshotPath, serializer, clock);
            
            string indexLockFileName = $"{snapshotPath}/{snapshotLogSettings.SnapshotLogName}_Index{snapshotLogSettings.FileExtension}";
            this.IndexLock = new AzureBlockBlobIndexLock(storageConnectionString, blobContainerName, indexLockFileName, serializer, clock);
        }

        protected override IClock Clock { get; }
        protected override ISnapshotLog<T> SnapshotLog { get; }
        protected override IIndexLock IndexLock { get; }
    }
}