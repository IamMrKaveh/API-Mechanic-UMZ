namespace Infrastructure.Common.Services;

public sealed class CacheIdempotencyService(ICacheService cacheService) : IIdempotencyService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string KeyPrefix = "idempotency:";

    public async Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        var cached = await cacheService.GetAsync<string>(BuildKey(idempotencyKey), ct);
        return !string.IsNullOrEmpty(cached);
    }

    public async Task<string?> GetResultAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        return await cacheService.GetAsync<string>(BuildKey(idempotencyKey), ct);
    }

    public async Task MarkAsProcessedAsync(Guid idempotencyKey, string result, CancellationToken ct = default)
    {
        await cacheService.SetAsync(BuildKey(idempotencyKey), result, DefaultTtl, ct);
    }

    private static string BuildKey(Guid idempotencyKey) => $"{KeyPrefix}{idempotencyKey:N}";
}