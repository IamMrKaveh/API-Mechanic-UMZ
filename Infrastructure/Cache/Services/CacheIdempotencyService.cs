namespace Infrastructure.Cache.Services;

public sealed class CacheIdempotencyService(ICacheService cacheService) : IIdempotencyService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24); private const string KeyPrefix = "idempotency:";

    public async Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty)
            return false;

        var key = BuildKey(idempotencyKey);
        var value = await cacheService.GetAsync<string>(key, ct);
        return value is not null;
    }

    public async Task MarkAsProcessedAsync(Guid idempotencyKey, string result, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty)
            return;

        var key = BuildKey(idempotencyKey);
        await cacheService.SetAsync(key, result ?? string.Empty, DefaultExpiry, ct);
    }

    public async Task<string?> GetResultAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty)
            return null;

        var key = BuildKey(idempotencyKey);
        return await cacheService.GetAsync<string>(key, ct);
    }

    private static string BuildKey(Guid idempotencyKey) => $"{KeyPrefix}{idempotencyKey:N}";
}