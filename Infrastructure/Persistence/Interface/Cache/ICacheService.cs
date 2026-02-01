namespace Infrastructure.Persistence.Interface.Cache;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;

    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, IEnumerable<string>? tags = null) where T : class;

    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

    Task ClearByPrefixAsync(string prefix);

    Task ClearByTagAsync(string tag);

    Task<bool> AcquireLockAsync(string key, TimeSpan expiry);

    Task<bool> AcquireLockWithRetryAsync(string key, TimeSpan expiry, int retryCount = 3, int retryDelayMs = 500);

    Task ReleaseLockAsync(string key);

    Task ClearAsync(string key);
}