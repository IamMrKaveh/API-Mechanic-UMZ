using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProducts;

public record GetProductsQuery(
    int? CategoryId,
    int? BrandId,
    string? Search,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<ProductListItemDto>>>;