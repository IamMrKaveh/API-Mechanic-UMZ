namespace Application.Product.Features.Queries.GetAdminProducts;

public record GetAdminProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<AdminProductListItemDto>>>;