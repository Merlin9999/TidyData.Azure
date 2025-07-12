 #nullable disable
 using System.Diagnostics;
 using System.Net;
 using System.Net.NetworkInformation;
 using Nito.AsyncEx;

 namespace TidyData.Azure.Tests
{
    public static class AzureStorageEmulatorManager
    {
        private static readonly AsyncLock LockObj = new();

        public static async Task EnsureStorageEmulatorIsStartedAsync(string workingFolder)
        {
            using (await LockObj.LockAsync())
            {
                if (!Directory.Exists(workingFolder))
                    Directory.CreateDirectory(workingFolder);

                if (!IsAzuriteRunning())
                {
                    ExecuteProcess(workingFolder);

                    for (int i = 0; i < 100; i++)
                    {
                        if (IsAzuriteRunning())
                            break;

                        await Task.Delay(100);
                    }

                    if (!IsAzuriteRunning())
                        throw new Exception("Failed to start Azurite Storage Emulator!");
                }
            }
        }

        public static bool IsRunning()
        {
            return IsAzuriteRunning();
        }

        // Adapted from: https://stackoverflow.com/a/66818289/677612
        private static bool IsAzuriteRunning()
        {
            // If Azurite is running, it will run on localhost and listen on port 10000 and/or 10001.
            IPAddress expectedIp = new(new byte[] { 127, 0, 0, 1 });
            var expectedPorts = new[] { 10000, 10001 };

            var activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

            var relevantListeners = activeTcpListeners.Where(t =>
                    expectedPorts.Contains(t.Port) &&
                    t.Address.Equals(expectedIp))
                .ToList();

            return relevantListeners.Any();
        }

        private static void ExecuteProcess(string workingFolder)
        {
            using (Process process = Process.Start(Create(workingFolder)))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Unable to start Azurite Storage Emulator process.");
                }
            }
        }

        private static ProcessStartInfo Create(string workingFolder)
        {
            string logFilePath = Path.Combine(workingFolder, "debug.log");

            return new ProcessStartInfo
            {
                FileName = @"powershell",
                Arguments = $"azurite.ps1 --location \"{workingFolder}\" --debug \"{logFilePath}\"",
                UseShellExecute = true,
            };
        }
    }
}
