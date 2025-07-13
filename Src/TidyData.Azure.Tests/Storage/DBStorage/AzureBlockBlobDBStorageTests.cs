 #nullable disable
 using NodaTime;
 using TidyData.Azure.Storage;
 using TidyData.SnapshotLog;
 using TidyData.Storage;
 using TidyData.Tests._Shared_Synced.Helpers;
 using TidyData.Tests._Shared_Synced.TestImpl;
 using TidyData.Tests._Shared_Synced.TestModel;
 using TidyUtility.Data.Json;

 namespace TidyData.Azure.Tests.Storage.DBStorage
{
    public class AzureBlockBlobDBStorageTests : IAsyncLifetime
    {
        private readonly string StorageConnectionString = "UseDevelopmentStorage=true";
        private readonly string TestContainerName = "unittests".BuildContainerName();
        private readonly string DBStoragePath = "CreateDBStorage";
        private readonly string BlockBlobExtension = ".json";

        public async Task InitializeAsync()
        {
            await AzureStorageEmulatorManager.EnsureStorageEmulatorIsStartedAsync(TestFolders.AzuriteFolder);
        }

        public Task DisposeAsync() { return Task.CompletedTask; }

        [Fact]
        public async Task Read()
        {
            await DBStorageTestImpl.ReadTestImplAsync(this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdate()
        {
            await DBStorageTestImpl.ReadUpdateTestImplAsync(this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdateFailsWhenAlreadyLockedWhenInitialStorageExists()
        {
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(true, this.DBStorageFactoryMethod);
        }

        [Fact]
        public async Task ReadUpdateFailsWhenAlreadyLockedWhenInitialStorageDoesNotExist()
        {
            await DBStorageTestImpl.ReadUpdateFailsWhenAlreadyLockedImplAsync(false, this.DBStorageFactoryMethod);
        }

        private IDBStorage<TestDataModel> DBStorageFactoryMethod(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration? maxSnapshotAgeToPreserveAll,
            ISerializer serializer, IClock clock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll ?? Duration.Zero, 
                FileExtension = this.BlockBlobExtension,
            };

            return new AzureBlockBlobDBStorage<TestDataModel>(snapshotLogSettings, StorageConnectionString, TestContainerName, DBStoragePath, serializer, clock);
        }
    }
}
