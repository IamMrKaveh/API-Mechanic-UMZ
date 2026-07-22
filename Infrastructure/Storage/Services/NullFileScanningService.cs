using Application.Storage.Contracts;

namespace Infrastructure.Storage.Services;

public sealed class NullFileScanningService : IFileScanningService
{
    public Task<FileScanResult> ScanAsync(Stream stream, string fileName, CancellationToken ct = default)
        => Task.FromResult(FileScanResult.Clean());
}
