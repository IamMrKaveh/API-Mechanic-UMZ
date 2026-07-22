namespace Application.Storage.Contracts;

public interface IFileMagicBytesValidator
{
    Task<bool> IsAllowedAsync(Stream stream, string declaredContentType, CancellationToken ct = default);
}
