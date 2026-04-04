namespace Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse>(ICacheService cacheService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService = cacheService;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next(ct);

        var cached = await _cacheService.GetAsync<TResponse>(cacheableQuery.CacheKey);
        if (cached is not null)
            return cached;

        var response = await next(ct);
        await _cacheService.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.CacheDuration);
        return response;
    }
}