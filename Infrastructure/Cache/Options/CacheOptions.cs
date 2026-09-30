namespace Infrastructure.Cache.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public bool IsEnabled { get; init; } = false;

    public bool UseRedis { get; init; } = false;

    public string RedisConnectionString { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int DefaultExpirationMinutes { get; init; } = 30;


    [Required(AllowEmptyStrings = false)]
    public string KeyPrefix { get; init; } = "shop";
}
