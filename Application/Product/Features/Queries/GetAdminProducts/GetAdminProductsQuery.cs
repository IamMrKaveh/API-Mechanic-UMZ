using SharedKernel.Models;

namespace Application.Product.Features.Queries.GetAdminProducts;

public record GetAdminProductsQuery(
    Guid? CategoryId,
    Guid? BrandId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<AdminProductListItemDto>>>;