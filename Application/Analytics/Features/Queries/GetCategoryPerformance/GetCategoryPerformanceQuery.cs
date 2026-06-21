using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed record GetCategoryPerformanceQuery(
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 10) : IPageQuery<CategoryPerformanceDto>;