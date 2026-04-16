namespace Infrastructure.Cache.Health;

public sealed class RedisCacheHealthCheck(
    IConnectionMultiplexer redis,
    IAuditService auditService,
    IOptions<CacheOptions> cacheOptions) : IHealthCheck
{
    private static readonly TimeSpan MaxAcceptableLatency = TimeSpan.FromMilliseconds(100);
    private static readonly string PingKey = "health:ping";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // اگر Cache غیرفعال است، Healthy برگردان
        if (!cacheOptions.Value.IsEnabled)
        {
            return HealthCheckResult.Healthy("Cache is disabled in configuration");
        }

        try
        {
            var db = redis.GetDatabase();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var pong = await db.PingAsync();
            sw.Stop();
            var latency = sw.Elapsed;

            var data = new Dictionary<string, object>
            {
                ["PingLatency"] = $"{pong.TotalMilliseconds:F1}ms",
                ["CheckLatency"] = $"{latency.TotalMilliseconds:F1}ms",
                ["ConnectedEndpoints"] = redis.GetEndPoints().Length,
                ["IsConnected"] = redis.IsConnected,
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
                await auditService.LogWarningAsync(
                    $"[RedisHealth] High latency: {latency.TotalMilliseconds}ms", ct);
                return HealthCheckResult.Degraded(
                    $"Redis latency is high: {latency.TotalMilliseconds:F1}ms",
                    data: data);
            }

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync("Redis health check failed.", ct);
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