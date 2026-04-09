using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed record GetCategoryPerformanceQuery(
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<ServiceResult<PaginatedResult<CategoryPerformanceDto>>>;