using Polly.CircuitBreaker;
using SharedContracts.Diagnostics;

namespace Infrastructure.Security.Services;

public sealed class ResilientRateLimitService : IRateLimitService
{
    private readonly RateLimitService _primary;
    private readonly InMemoryRateLimitService _fallback;
    private readonly BusinessMetrics _metrics;
    private readonly ILogger<ResilientRateLimitService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public ResilientRateLimitService(
        RateLimitService primary,
        InMemoryRateLimitService fallback,
        BusinessMetrics metrics,
        ILogger<ResilientRateLimitService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _metrics = metrics;
        _logger = logger;

        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (ex, breakDelay) =>
                    _logger.LogWarning(ex, "Rate-limit circuit opened for {Delay} due to Redis failure.", breakDelay),
                onReset: () =>
                    _logger.LogInformation("Rate-limit circuit reset; Redis is healthy."),
                onHalfOpen: () =>
                    _logger.LogInformation("Rate-limit circuit half-open; probing Redis."));
    }

    public async Task<(bool IsLimited, TimeSpan? RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxAttempts,
        int windowUnits)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(
                () => _primary.IsLimitedAsync(key, maxAttempts, windowUnits));
        }
        catch (BrokenCircuitException)
        {
            RecordFallback("circuit_open");
            return await _fallback.IsLimitedAsync(key, maxAttempts, windowUnits);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate-limit failed for key {Key}; falling back to in-memory.", key);
            RecordFallback("redis_error");
            return await _fallback.IsLimitedAsync(key, maxAttempts, windowUnits);
        }
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _primary.ResetAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to reset Redis rate-limit key {Key}.",
                key,
                ct);
        }
    }

    private void RecordFallback(string reason)
    {
        _metrics.RateLimitFallbackActive.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }
}
