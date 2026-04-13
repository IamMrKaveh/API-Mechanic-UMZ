using Application.Cache.Interfaces;

namespace Application.Common.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    IAuditService auditService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next(ct);

        var cached = await cacheService.GetAsync<TResponse>(cacheableQuery.CacheKey, ct);
        if (cached is not null)
        {
            await auditService.LogSystemEventAsync(
                "Cache hit",
                $"Cache hit for {cacheableQuery.CacheKey}",
                ct);
            return cached;
        }

        var response = await next(ct);

        if (response is not null)
            await cacheService.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.Expiry, ct);

        return response;
    }
}