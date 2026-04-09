namespace Application.Product.Features.Queries.GetProductCatalog;

public record GetProductCatalogQuery(
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null) : IRequest<ServiceResult<PaginatedResult<ProductCatalogItemDto>>>;