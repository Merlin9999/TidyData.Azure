 #nullable disable
 using NodaTime;
 using TidyData.Azure.SnapshotLog;
 using TidyData.SnapshotLog;
 using TidyData.Storage;
 using TidyUtility.Data.Json;

 namespace TidyData.Azure.Storage
{
    public sealed class AzureBlockBlobDBStorage<T> : DBStorageBase<T>
        where T : class, new()
    {
        public AzureBlockBlobDBStorage(SnapshotLogSettings settings, 
            string storageConnectionString, string blobContainerName, string snapshotPath,
            ISerializer serializer, IClock clock = null)
        {
            if (string.IsNullOrWhiteSpace(settings.SnapshotLogName))
                throw new ArgumentException($"{nameof(SnapshotLogSettings)}.{nameof(settings.SnapshotLogName)} must not be null or empty!", nameof(settings.SnapshotLogName));

            snapshotPath = snapshotPath.Replace('\\', '/').TrimEnd('/');

            this.Clock = clock ?? SystemClock.Instance;
            this.SnapshotLog = new AzureBlockBlobSnapshotLog<T>(settings, 
                storageConnectionString, blobContainerName, snapshotPath, serializer, clock);
            
            string indexLockFileName = $"{snapshotPath}/{settings.SnapshotLogName}_Index{settings.FileExtension}";
            this.IndexLock = new AzureBlockBlobIndexLock(storageConnectionString, blobContainerName, indexLockFileName, serializer, clock);
        }

        protected override IClock Clock { get; }
        protected override ISnapshotLog<T> SnapshotLog { get; }
        protected override IIndexLock IndexLock { get; }
    }
}