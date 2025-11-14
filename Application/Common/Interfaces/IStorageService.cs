namespace Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string folder, int? entityId = null);
    Task DeleteFileAsync(string relativePath);
    string GetFileUrl(string relativePath);
}