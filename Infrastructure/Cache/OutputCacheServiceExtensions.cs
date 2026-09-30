using Infrastructure.Cache.Serialization;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.EntityFramework.Configurations;
using NexGen.MediatR.Extensions.Caching.Redis.Configurations;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using JsonSerializerSettings = Newtonsoft.Json.JsonSerializerSettings;

namespace Infrastructure.Cache;

internal static class OutputCacheServiceExtensions
{
    /// <summary>
    /// Registers the opt-in MediatR output cache (<c>[RequestOutputCache]</c> /
    /// <c>[RequestOutputCacheEvict]</c>). Redis is used when <see cref="CacheOptions.UseRedis"/> is
    /// enabled (with Pub/Sub tag eviction across instances); otherwise a process-local memory cache.
    /// Requests without the attribute are unaffected and keep using <c>ICacheableQuery</c>.
    /// </summary>
    public static IServiceCollection AddMediatRResponseCache(
        this IServiceCollection services,
        IConfiguration configuration,
        CacheOptions cacheOptions,
        string redisConnectionString)
    {
        RegisterServiceResultConverter();

        services.AddMediatROutputCache(options =>
        {
            if (cacheOptions.UseRedis)
            {
                options.UseRedisCache(redis =>
                {
                    redis.ConnectionString = redisConnectionString;
                    redis.InstanceName = $"{cacheOptions.KeyPrefix}:mediatr:";
                    redis.EnableDistributedEviction = true;
                });
            }
            else
            {
                options.UseMemoryCache();
            }
        });

        return services;
    }

    /// <summary>
    /// Adds the EF Core interceptor that evicts cache tags matching the entity type names
    /// changed by <c>SaveChanges</c>.
    /// </summary>
    public static void UseResponseCacheAutoEvict(
        this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
        => options.UseMediatROutputCacheAutoEvict(serviceProvider);

    private static int _converterRegistered;

    private static void RegisterServiceResultConverter()
    {
        if (Interlocked.Exchange(ref _converterRegistered, 1) == 1)
            return;

        var previous = JsonConvert.DefaultSettings;

        JsonConvert.DefaultSettings = () =>
        {
            var settings = previous?.Invoke() ?? new JsonSerializerSettings();
            settings.Converters.Add(new ServiceResultJsonConverter());
            return settings;
        };
    }
}
