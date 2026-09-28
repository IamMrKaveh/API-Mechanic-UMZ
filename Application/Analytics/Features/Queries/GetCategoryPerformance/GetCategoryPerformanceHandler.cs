using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetCategoryPerformanceQuery, PaginatedResult<CategoryPerformanceDto>>
{
    public async Task<ServiceResult<PaginatedResult<CategoryPerformanceDto>>> Handle(
        GetCategoryPerformanceQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetCategoryPerformanceAsync(
            request.FromDate, request.ToDate, ct);

        return ServiceResult<PaginatedResult<CategoryPerformanceDto>>.Success(result);
    }
}
