namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsHandler
    : IRequestHandler<GetTopSellingProductsQuery, ServiceResult<IReadOnlyList<TopSellingProductDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;

    public GetTopSellingProductsHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache)
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
    }

    public async Task<ServiceResult<IReadOnlyList<TopSellingProductDto>>> Handle(
        GetTopSellingProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:top-products:{request.Count}:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await _cache.GetAsync<IReadOnlyList<TopSellingProductDto>>(cacheKey);
        if (cached is not null)
            return ServiceResult<IReadOnlyList<TopSellingProductDto>>.Success(cached);

        var result = await _analyticsQuery.GetTopSellingProductsAsync(
            request.Count, request.FromDate, request.ToDate, cancellationToken);

        //await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<IReadOnlyList<TopSellingProductDto>>.Success(result);
    }
}