namespace Application.Storage.Features.Shared;

public class LiaraStorageSettings
{
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? BucketName { get; init; }
    public string? BaseUrl { get; init; }
    public string? ApiEndpoint { get; init; }
}