using Application.Product.Features.Shared;

namespace Presentation.Product.Requests;

public record GetAdminProductsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    bool? IsActive = null,
    bool IncludeDeleted = false
);

public record GetProductCatalogRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null);

public record CreateProductRequest(
    string Name,
    string Slug,
    string Description,
    decimal Price,
    Guid CategoryId,
    Guid BrandId
);

public record UpdateProductRequest(
    Guid ProductId,
    Guid CategoryId,
    Guid BrandId,
    string Name,
    decimal Price,
    string Slug,
    string? Description,
    bool IsActive,
    bool IsFeatured,
    string RowVersion
);

public record UpdateProductDetailsRequest(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    bool IsActive,
    string? Sku,
    string RowVersion
);

public record BulkUpdatePricesRequest(
    ICollection<VariantPriceUpdateInput> Updates);