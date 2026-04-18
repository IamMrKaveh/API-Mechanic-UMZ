namespace Infrastructure.Storage.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public long MaxFileSizeBytes { get; init; } = 10_485_760;
}