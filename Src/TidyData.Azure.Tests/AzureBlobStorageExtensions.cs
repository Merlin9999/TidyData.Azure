#nullable disable
namespace TidyData.Azure.Tests;

public static class AzureBlobStorageExtensions
{
    public static string BuildContainerName(this string identifier)
    {
        return $"tidydata-{identifier.ToLower()}";
    }

}