namespace Application.Storage.Contracts;

public interface IFileScanningService
{
    Task<FileScanResult> ScanAsync(Stream stream, string fileName, CancellationToken ct = default);
}

public sealed record FileScanResult(bool IsClean, string? ThreatName, string? EngineMessage)
{
    public static FileScanResult Clean() => new(true, null, null);
    public static FileScanResult Infected(string threatName, string? engineMessage = null)
        => new(false, threatName, engineMessage);
}
