using Application.Media.Features.Shared;
using Application.Variant.Features.Shared;

namespace Application.Product.Features.Shared;

public sealed record AdminProductSearchParams(
    string? Name,
    int? CategoryId,
    int? BrandId,
    bool? IsActive,
    bool IncludeDeleted,
    int Page,
    int PageSize
);

public sealed record AdminProductListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public int TotalStock { get; init; }
    public int VariantCount { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record AdminProductDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int BrandId { get; init; }
    public string? IconUrl { get; init; }
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public IEnumerable<ProductVariantViewDto> Variants { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record ProductCatalogSearchParams(
    string? Search,
    int? CategoryId,
    int? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStockOnly,
    string? SortBy,
    int Page,
    int PageSize
);

public sealed record ProductCatalogItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public bool IsInStock { get; init; }
    public bool HasDiscount { get; init; }
    public decimal MaxDiscountPercentage { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

public sealed record ProductSummaryDto(
    int Id,
    string Name,
    string? Sku,
    string? IconUrl,
    decimal MinPrice,
    decimal MaxPrice,
    bool IsActive,
    decimal SellingPrice,
    decimal PurchasePrice,
    string? Icon,
    int Count,
    bool IsInStock
);

public sealed record PublicProductDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int BrandId { get; init; }
    public BrandInfoDto? Brand { get; init; }
    public string? IconUrl { get; init; }
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public IEnumerable<ProductVariantViewDto> Variants { get; init; } = [];
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public int TotalStock { get; init; }
    public bool HasMultipleVariants { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

/// <summary>
/// DTO قدیمی ادمین - برای Handlerهای Legacy
/// </summary>
public sealed record AdminProductViewDto(
    int Id,
    string Name,
    string? Description,
    string? Sku,
    int BrandId,
    bool IsActive,
    bool IsDeleted,
    string? IconUrl,
    IEnumerable<MediaDto> Images,
    List<ProductVariantResponseDto> Variants,
    string? RowVersion,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    BrandSummaryInProductDto Brand
);

/// <summary>
/// DTO عمومی محصول - برای Handlerهای Legacy
/// </summary>
public sealed record PublicProductViewDto(
    int Id,
    string Name,
    string? Description,
    string? Sku,
    int BrandId,
    string? IconUrl,
    IEnumerable<Application.Media.Features.Shared.MediaDto> Images,
    List<ProductVariantResponseDto> Variants,
    decimal MinPrice,
    decimal MaxPrice,
    int TotalStock,
    BrandSummaryInProductDto Brand
);

public sealed record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int BrandId { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record AdminProductListDto(
    int Id,
    string Name,
    string? Sku,
    int BrandId,
    string? BrandName,
    string? CategoryName,
    bool IsActive,
    bool IsFeatured,
    decimal MinPrice,
    decimal MaxPrice,
    int TotalStock,
    decimal AverageRating,
    int ReviewCount,
    int SalesCount,
    int VariantCount,
    string? IconUrl,
    DateTime CreatedAt,
    string? RowVersion
);

public sealed record ProductSearchParams(
    string? SearchTerm,
    int? CategoryId,
    int? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? IsActive,
    bool? IsFeatured,
    bool? InStock,
    string? SortBy,
    bool SortDescending,
    int Page,
    int PageSize
);

public sealed record CreateProductInput
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public int BrandId { get; init; }
    public string? Description { get; init; }
}

public sealed record UpdateProductInput(
    int Id,
    string Name,
    string Slug,
    int CategoryId,
    int BrandId,
    string? Description,
    bool IsActive,
    string? Sku,
    string RowVersion,
    List<UploadImageInput> Images
);

public sealed record UpdateProductDetailsRequest(
    string Name,
    string Description,
    int CategoryId,
    int BrandId);

public sealed record ProductPriceUpdateItem(int ProductId, decimal NewPrice);