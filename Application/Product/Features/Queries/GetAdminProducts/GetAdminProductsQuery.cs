namespace Application.Product.Features.Queries.GetAdminProducts;

public record GetAdminProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted) : IRequest<ServiceResult<PaginatedResult<AdminProductListItemDto>>>;