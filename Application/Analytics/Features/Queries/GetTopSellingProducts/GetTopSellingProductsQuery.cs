using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed record GetTopSellingProductsQuery(
    int Count = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<ServiceResult<PaginatedResult<TopSellingProductDto>>>;