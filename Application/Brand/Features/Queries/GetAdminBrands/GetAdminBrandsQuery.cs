namespace Application.Brand.Features.Queries.GetAdminBrands;

public record GetAdminBrandsQuery(
    int? CategoryId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<BrandListItemDto>>>;