using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProducts;

public record GetAdminProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    Guid UserId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page = 1,
    int PageSize = 20
) : IRequest<ServiceResult<PaginatedResult<ProductListItemDto>>>, IQuery;