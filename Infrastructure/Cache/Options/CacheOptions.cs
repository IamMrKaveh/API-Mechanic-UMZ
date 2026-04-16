namespace Infrastructure.Cache.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public bool IsEnabled { get; init; } = false;
    public string RedisConnectionString { get; init; } = string.Empty;
    public int DefaultExpirationMinutes { get; init; } = 30;
    public int ShortExpirationMinutes { get; init; } = 5;
    public int LongExpirationMinutes { get; init; } = 120;
    public string KeyPrefix { get; init; } = "shop";
}