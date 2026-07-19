using Infrastructure.Communication.Options;

namespace Infrastructure.Communication.HealthChecks;

public sealed class KavenegarHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<KavenegarOptions> options,
    IMemoryCache cache) : IHealthCheck
{
    private const string CacheKey = "healthcheck:kavenegar";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out HealthCheckResult cached))
            return cached;

        var apiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var missing = HealthCheckResult.Unhealthy("Kavenegar API key is not configured.");
            cache.Set(CacheKey, missing, CacheDuration);
            return missing;
        }

        HealthCheckResult result;
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var url = $"https://api.kavenegar.com/v1/{apiKey}/account/info.json";
            using var response = await client.GetAsync(url, cancellationToken);

            result = response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Kavenegar reachable.")
                : HealthCheckResult.Degraded($"Kavenegar returned HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = HealthCheckResult.Unhealthy("Kavenegar unreachable.", ex);
        }

        cache.Set(CacheKey, result, CacheDuration);
        return result;
    }
}
