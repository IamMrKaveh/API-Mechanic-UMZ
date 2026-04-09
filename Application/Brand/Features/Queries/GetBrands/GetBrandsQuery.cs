using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetBrands;

public record GetBrandsQuery(
    int? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<BrandListItemDto>>>;