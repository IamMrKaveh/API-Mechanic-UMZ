namespace Application.Media.Contracts;

public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string? folder = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(string filePath, CancellationToken ct = default);

    Task<bool> ExistsAsync(string filePath, CancellationToken ct = default);

    string GetPublicUrl(string filePath);
}