 #nullable disable
 using NodaTime;
 using NodaTime.Testing;
 using TidyData.Azure.SnapshotLog;
 using TidyData.Azure.Tests.Helpers;
 using TidyData.SnapshotLog;
 using TidyData.Tests._Shared_Synced.Helpers;
 using TidyData.Tests._Shared_Synced.SnapshotLog;
 using TidyData.Tests._Shared_Synced.TestImpl;

 namespace TidyData.Azure.Tests.SnapshotLog
{
    public class AzureBlockBlobSnapshotLogTests : IAsyncLifetime
    {
        private readonly string StorageConnectionString = "UseDevelopmentStorage=true";
        private readonly string TestContainerName = "unittests".BuildContainerName();
        private readonly string FileSetExtension = ".json";
        private readonly string _fileSetRootFolderPath = "SnapshotLog";

        public async Task InitializeAsync()
        {
            if (!EnvironmentHelpers.IsRunningOnServer())
                await AzureStorageEmulatorManager.EnsureStorageEmulatorIsStartedAsync(TestFolders.AzuriteFolder);
        }

        public Task DisposeAsync() { return Task.CompletedTask; }

        [Fact]
        public async Task LoadEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task SaveAndLoadLastSavedImmutableDataToSnapshotAsync()
        {
            await SnapshotLogTestsImpl.SaveAndLoadLastSavedImmutableDataToSnapshotAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task SaveAndLoadNamedImmutableDataToSnapshotAsync()
        {
            await SnapshotLogTestsImpl.SaveAndLoadNamedImmutableDataToSnapshotAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadNonExistingNamedSnapshotFromEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.LoadNonExistingNamedSnapshotFromNonEmptySnapshotLogAsync(this.ConstructSnapshotLog);
        }

        [Fact]
        public async Task MaxCountInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxCountInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCount);
        }

        [Fact]
        public async Task MaxAgeInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxAgeInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxAge);
        }

        [Fact]
        public async Task MaxCountAndPreserveByHourAndDayInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxCountAndPreserveByHourAndDayInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCountAndPreserveByHourAndDay);
        }

        [Fact]
        public async Task MaxAgeAndMaxCountInSnapshotLogAsync()
        {
            await SnapshotLogTestsImpl.MaxAgeAndMaxCountInSnapshotLogAsync(this.ConstructSnapshotLogWithMaxCountAndAge);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLog(string snapshotLogName)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                FileExtension = this.FileSetExtension,
            };

            if (EnvironmentHelpers.IsRunningOnServer())
                return new MemorySnapshotLog<ImmutableData>(snapshotLogSettings);
            return new AzureBlockBlobSnapshotLog<ImmutableData>(snapshotLogSettings, StorageConnectionString, TestContainerName, this._fileSetRootFolderPath);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCount(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                FileExtension = this.FileSetExtension,
            };

            if (EnvironmentHelpers.IsRunningOnServer())
                return new MemorySnapshotLog<ImmutableData>(snapshotLogSettings);
            return new AzureBlockBlobSnapshotLog<ImmutableData>(snapshotLogSettings, StorageConnectionString, TestContainerName, this._fileSetRootFolderPath);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxAge(string snapshotLogName,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                FileExtension = this.FileSetExtension,
            };

            if (EnvironmentHelpers.IsRunningOnServer())
                return new MemorySnapshotLog<ImmutableData>(snapshotLogSettings, clock: fakeClock);
            return new AzureBlockBlobSnapshotLog<ImmutableData>(snapshotLogSettings, StorageConnectionString, TestContainerName, this._fileSetRootFolderPath, clock: fakeClock);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndAge(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion,
            Duration maxSnapshotAgeToPreserveAll, FakeClock fakeClock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                FileExtension = this.FileSetExtension,
            };

            if (EnvironmentHelpers.IsRunningOnServer())
                return new MemorySnapshotLog<ImmutableData>(snapshotLogSettings, clock: fakeClock);
            return new AzureBlockBlobSnapshotLog<ImmutableData>(snapshotLogSettings, StorageConnectionString, TestContainerName, this._fileSetRootFolderPath, clock: fakeClock);
        }

        private ISnapshotLog<ImmutableData> ConstructSnapshotLogWithMaxCountAndPreserveByHourAndDay(string snapshotLogName,
            int minSnapshotCountBeforeEligibleForDeletion, Duration maxSnapshotAgeToPreserveAll,
            Duration maxSnapshotAgeToPreserveOnePerHour, Duration maxSnapshotAgeToPreserveOnePerDay, FakeClock fakeClock)
        {
            var snapshotLogSettings = new SnapshotLogSettings()
            {
                SnapshotLogName = snapshotLogName,
                MinSnapshotCountBeforeEligibleForDeletion = minSnapshotCountBeforeEligibleForDeletion,
                MaxSnapshotAgeToPreserveAll = maxSnapshotAgeToPreserveAll,
                MaxSnapshotAgeToPreserveOnePerHour = maxSnapshotAgeToPreserveOnePerHour,
                MaxSnapshotAgeToPreserveOnePerDay = maxSnapshotAgeToPreserveOnePerDay, 
                FileExtension = this.FileSetExtension,
            };

            if (EnvironmentHelpers.IsRunningOnServer())
                return new MemorySnapshotLog<ImmutableData>(snapshotLogSettings, clock: fakeClock);
            return new AzureBlockBlobSnapshotLog<ImmutableData>(snapshotLogSettings, StorageConnectionString, TestContainerName, this._fileSetRootFolderPath, clock: fakeClock);
        }
    }
}
