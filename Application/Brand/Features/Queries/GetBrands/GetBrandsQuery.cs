namespace Application.Brand.Features.Queries.GetBrands;

public record GetAdminBrandsLegacyQuery(
    int? CategoryId,
    string? Search,
    int Page,
    int PageSize
    )
    : IRequest<ServiceResult<PaginatedResult<BrandListItemDto>>>;