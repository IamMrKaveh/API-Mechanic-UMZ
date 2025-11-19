namespace Application.Common.Interfaces;

public interface IStorageService
{
    Task<(string FilePath, string FileName)> SaveFileAsync(Stream stream, string fileName, string entityType, string entityId);
    Task DeleteFileAsync(string relativePath);
    string GetUrl(string? relativePath);
}