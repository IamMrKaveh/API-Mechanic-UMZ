using Infrastructure.Payment.ZarinPal.Options;
using HttpMethod = System.Net.Http.HttpMethod;

namespace Infrastructure.Payment.ZarinPal.HealthChecks;

public sealed class ZarinPalHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<ZarinPalOptions> options,
    IMemoryCache cache) : IHealthCheck
{
    private const string CacheKey = "healthcheck:zarinpal";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out HealthCheckResult cached))
            return cached;

        var settings = options.Value;
        var baseUrl = settings.UseSandbox ? settings.SandboxApiBaseUrl : settings.ApiBaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            var missing = HealthCheckResult.Unhealthy("ZarinPal API base URL is not configured.");
            cache.Set(CacheKey, missing, CacheDuration);
            return missing;
        }

        HealthCheckResult result;
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(HttpMethod.Head, baseUrl);
            using var response = await client.SendAsync(request, cancellationToken);

            result = HealthCheckResult.Healthy($"ZarinPal endpoint reachable (HTTP {(int)response.StatusCode}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            result = HealthCheckResult.Unhealthy("ZarinPal endpoint unreachable.", ex);
        }

        cache.Set(CacheKey, result, CacheDuration);
        return result;
    }
}
