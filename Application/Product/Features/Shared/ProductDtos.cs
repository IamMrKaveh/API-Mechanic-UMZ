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

public sealed record BrandInfoDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

public sealed record ProductVariantViewDto
{
    public int Id { get; init; }
    public string? Sku { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsActive { get; init; }
    public bool IsInStock { get; init; }
    public bool HasDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public Dictionary<string, AttributeValueDto> Attributes { get; init; } = [];
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public string? RowVersion { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<int> EnabledShippingIds { get; init; } = [];
}

public sealed record AttributeValueDto(
    int Id,
    string TypeName,
    string TypeDisplayName,
    string Value,
    string DisplayValue,
    string? HexCode
);

public sealed record AttributeTypeWithValuesDto(
    int Id,
    string Name,
    string DisplayName,
    int SortOrder,
    bool IsActive,
    List<AttributeValueSimpleDto> Values
);

public sealed record AttributeValueSimpleDto(
    int Id,
    string Value,
    string DisplayValue,
    string? HexCode,
    int SortOrder,
    bool IsActive
);

public sealed record CreateProductVariantInput(
    int? Id,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    bool IsActive,
    List<int> AttributeValueIds,
    decimal ShippingMultiplier,
    List<int>? EnabledShippingIds
);

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
    IEnumerable<Application.Media.Features.Shared.MediaDto> Images,
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

public sealed record ProductVariantResponseDto(
    int Id,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    bool IsActive,
    decimal ShippingMultiplier,
    List<int> EnabledShippingIds,
    Dictionary<string, AttributeValueDto> Attributes,
    IEnumerable<Application.Media.Features.Shared.MediaDto> Images,
    string? RowVersion,
    bool IsInStock,
    bool HasDiscount,
    int DiscountPercentage
);

public sealed record CreateProductVariantDto(
    int? Id,
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock,
    bool IsUnlimited,
    int? LowStockThreshold,
    bool IsActive,
    decimal ShippingMultiplier,
    List<int> AttributeValueIds,
    List<int>? EnabledShippingIds
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

public sealed record BrandSummaryInProductDto(
    int Id,
    string Name,
    string CategoryName
);

public sealed record UpdateProductDetailsRequest(
    string Name,
    string Description,
    int CategoryId,
    int BrandId);

public sealed record ChangePriceRequest(decimal NewPrice);

public sealed record BulkUpdatePricesRequest(IReadOnlyList<ProductPriceUpdateItem> Items);

public sealed record ProductPriceUpdateItem(int ProductId, decimal NewPrice);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int CategoryId,
    int BrandId);