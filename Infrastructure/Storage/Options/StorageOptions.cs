namespace Infrastructure.Storage.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; init; } = "S3";
    public string BucketName { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public bool ForcePathStyle { get; init; } = true;
    public bool UseHttp { get; init; } = false;
    public long MaxFileSizeBytes { get; init; } = 10_485_760;
}