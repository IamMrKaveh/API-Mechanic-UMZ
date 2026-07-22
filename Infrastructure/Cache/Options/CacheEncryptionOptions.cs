namespace Infrastructure.Cache.Options;

public sealed class CacheEncryptionOptions
{
    public const string SectionName = "Cache:Encryption";

    public bool IsEnabled { get; init; } = false;

    public string KeyBase64 { get; init; } = string.Empty;

    public string KeyId { get; init; } = "v1";

    public bool UseTls { get; init; } = true;
}
