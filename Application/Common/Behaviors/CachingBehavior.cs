using Application.Cache.Contracts;
using Application.Cache.Interfaces;

namespace Application.Common.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
            return await next();

        var cached = await cacheService.GetAsync<TResponse>(cacheable.CacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for {Key}", cacheable.CacheKey);
            return cached;
        }

        var response = await next();

        if (response is not null)
            await cacheService.SetAsync(cacheable.CacheKey, response, cacheable.Expiry, ct);

        return response;
    }
}