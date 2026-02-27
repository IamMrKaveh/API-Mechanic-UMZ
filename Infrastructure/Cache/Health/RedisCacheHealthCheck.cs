using Infrastructure.Cache.Options;

namespace Infrastructure.Cache.Health;

public sealed class RedisCacheHealthCheck : IHealthCheck
{
    private static readonly TimeSpan MaxAcceptableLatency = TimeSpan.FromMilliseconds(100);
    private static readonly string PingKey = "health:ping";
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheHealthCheck> _logger;
    private readonly CacheOptions _cacheOptions;

    public RedisCacheHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheHealthCheck> logger,
        IOptions<CacheOptions> cacheOptions)
    {
        _redis = redis;
        _logger = logger;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // اگر Cache غیرفعال است، Healthy برگردان
        if (!_cacheOptions.IsEnabled)
        {
            return HealthCheckResult.Healthy("Cache is disabled in configuration");
        }

        try
        {
            var db = _redis.GetDatabase();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var pong = await db.PingAsync();
            sw.Stop();
            var latency = sw.Elapsed;

            var data = new Dictionary<string, object>
            {
                ["PingLatency"] = $"{pong.TotalMilliseconds:F1}ms",
                ["CheckLatency"] = $"{latency.TotalMilliseconds:F1}ms",
                ["ConnectedEndpoints"] = _redis.GetEndPoints().Length,
                ["IsConnected"] = _redis.IsConnected,
            };

            var testValue = $"ok-{DateTime.UtcNow:HHmmss}";
            await db.StringSetAsync(PingKey, testValue, TimeSpan.FromSeconds(5));
            var readBack = await db.StringGetAsync(PingKey);

            if (readBack != testValue)
            {
                return HealthCheckResult.Degraded(
                    "Redis write/read mismatch",
                    data: data);
            }

            if (latency > MaxAcceptableLatency)
            {
                _logger.LogWarning(
                    "[RedisHealth] High latency: {Latency}ms", latency.TotalMilliseconds);
                return HealthCheckResult.Degraded(
                    $"Redis latency is high: {latency.TotalMilliseconds:F1}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RedisHealth] Redis health check failed.");
            return HealthCheckResult.Unhealthy(
                "Redis is unreachable",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["Type"] = ex.GetType().Name
                });
        }
    }
}