using Application.Product.Features.Shared;

namespace Presentation.Product.Requests;

public record BulkUpdatePricesRequest(
    ICollection<VariantPriceUpdateInput> Updates);

public record GetAdminProductsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    bool? IsActive = null,
    bool IncludeDeleted = false
);

public record GetAdminProductByIdRequest();

public record GetProductsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null);

public record GetProductRequest();

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

public record GetProductDetailsRequest();

public record ChangePriceRequest(
    Guid VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice);

public record ProductCatalogSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false,
    string? SortBy = null
);

public record CreateProductRequest(
    string Name,
    string Slug,
    string Description,
    decimal Price,
    Guid CategoryId,
    Guid BrandId
);

public record UpdateProductRequest(
    string Name,
    decimal Price,
    string Slug,
    string? Description,
    Guid CategoryId,
    Guid BrandId,
    bool IsActive,
    bool IsFeatured,
    string RowVersion
);

public record ActiveProductRequest();

public record DeactiveProductRequest();

public record DeleteProductRequest();

public record RestoreProductRequest();

public record UpdateProductDetailsRequest(
    string Name,
    string? Description,
    Guid BrandId,
    bool IsActive,
    string? Sku,
    string RowVersion
);