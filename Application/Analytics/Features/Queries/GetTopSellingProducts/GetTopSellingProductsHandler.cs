using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetTopSellingProductsQuery, PaginatedResult<TopSellingProductDto>>
{
    public async Task<ServiceResult<PaginatedResult<TopSellingProductDto>>> Handle(
        GetTopSellingProductsQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetTopSellingProductsAsync(
            request.Count, request.FromDate, request.ToDate, ct);

        return ServiceResult<PaginatedResult<TopSellingProductDto>>.Success(result);
    }
}
