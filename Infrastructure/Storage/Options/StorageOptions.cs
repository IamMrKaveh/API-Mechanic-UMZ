namespace Infrastructure.Storage.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required(AllowEmptyStrings = false)]
    public string Provider { get; init; } = "S3";

    [Required(AllowEmptyStrings = false)]
    public string BucketName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Region { get; init; } = string.Empty;

    public bool ForcePathStyle { get; init; } = true;

    public bool UseHttp { get; init; } = false;

    [Range(1, 1_073_741_824)]
    public long MaxFileSizeBytes { get; init; } = 10_485_760;
}