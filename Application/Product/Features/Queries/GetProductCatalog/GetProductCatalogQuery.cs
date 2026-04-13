using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProductCatalog;

public record GetProductCatalogQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null) : IRequest<ServiceResult<PaginatedResult<ProductCatalogItemDto>>>;