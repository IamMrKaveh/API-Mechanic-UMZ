namespace Infrastructure.Cache.Health;

/// <summary>
/// Health Check برای Redis Cache.
/// بررسی می‌کند که:
/// 1. اتصال به Redis برقرار است
/// 2. عملیات نوشتن و خواندن کار می‌کند
/// 3. Latency در محدوده قابل قبول است
/// </summary>
public sealed class RedisCacheHealthCheck : IHealthCheck
{
    private static readonly TimeSpan MaxAcceptableLatency = TimeSpan.FromMilliseconds(100);
    private static readonly string PingKey = "health:ping";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheHealthCheck> _logger;

    public RedisCacheHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
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

            
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var serverInfo = await server.InfoAsync("server");
                var version = serverInfo
                    .SelectMany(g => g)
                    .FirstOrDefault(kv => kv.Key == "redis_version")
                    .Value;

                data["RedisVersion"] = version ?? "unknown";
                data["UsedMemoryMb"] = GetMemoryUsage(server);
            }
            catch {  }

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

    private static string GetMemoryUsage(IServer server)
    {
        try
        {
            var memInfo = server.Info("memory");
            var usedMemory = memInfo
                .SelectMany(g => g)
                .FirstOrDefault(kv => kv.Key == "used_memory_human")
                .Value;
            return usedMemory ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}