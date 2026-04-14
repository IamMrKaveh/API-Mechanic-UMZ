namespace Infrastructure.Storage.Options;

public sealed class S3Options
{
    public const string SectionName = "AWS:S3";

    public string BucketName { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}