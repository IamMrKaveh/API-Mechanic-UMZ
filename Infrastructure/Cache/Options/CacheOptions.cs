namespace Infrastructure.Cache.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// آیا سرویس Cache (Redis) فعال است
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public int DefaultTtlMinutes { get; set; } = 5;
    public int LockTtlSeconds { get; set; } = 30;
}