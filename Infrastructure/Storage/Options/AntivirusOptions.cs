namespace Infrastructure.Storage.Options;

public sealed class AntivirusOptions
{
    public const string SectionName = "Storage:Antivirus";

    public bool IsEnabled { get; init; } = false;

    [Required(AllowEmptyStrings = false)]
    public string Host { get; init; } = "127.0.0.1";

    [Range(1, 65535)]
    public int Port { get; init; } = 3310;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(1024, 104_857_600)]
    public int ChunkSizeBytes { get; init; } = 64 * 1024;

    public bool FailClosedOnEngineError { get; init; } = true;
}
