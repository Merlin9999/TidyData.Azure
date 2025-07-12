#nullable disable
using TidyData.Tests.Azure;

namespace TidyData.Tests.Azure;

public static class AzureBlobStorageExtensions
{
    public static string BuildContainerName(this string identifier)
    {
        return $"tidydata-{identifier.ToLower()}";
    }

}