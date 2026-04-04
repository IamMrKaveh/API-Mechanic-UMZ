namespace Application.Cache.Contracts;

public interface ICacheService
{
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken ct = default) where T : class;

    Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null) where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default) where T : class;

    Task RemoveAsync(
        string key,
        CancellationToken ct = default);

    Task RemoveByPrefixAsync(
        string prefix,
        CancellationToken ct = default);

    Task ClearAsync(string key);

    Task ClearByPrefixAsync(string prefix);

    Task<bool> AcquireLockAsync(
        string key,
        TimeSpan expiry);

    Task ReleaseLockAsync(string key);

    Task<bool> AcquireLockWithRetryAsync(
        string key,
        TimeSpan expiry,
        int retryCount = 3,
        int retryDelayMs = 500);
}