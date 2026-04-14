namespace Infrastructure.Storage.Options;

public sealed class LiaraStorageSettings
{
    public const string SectionName = "Liara";

    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiEndpoint { get; init; } = string.Empty;
}