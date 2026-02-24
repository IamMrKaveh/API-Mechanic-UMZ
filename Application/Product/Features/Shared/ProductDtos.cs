namespace Application.Product.Features.Shared;



public record AdminProductSearchParams
{
    public string? Name { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public bool? IsActive { get; init; }
    public bool IncludeDeleted { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record AdminProductListItemDto
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

public record AdminProductDetailDto
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



public record ProductCatalogSearchParams
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    public string? SortBy { get; init; } 
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record ProductCatalogItemDto
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

public record ProductSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? IconUrl { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public bool IsActive { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal PurchasePrice { get; init; }
    public string? Icon { get; init; }
    public int Count { get; init; }
    public bool IsInStock { get; init; }
}

public record PublicProductDetailDto
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

public record BrandInfoDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}



public record ProductVariantViewDto
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
    public Dictionary<string, AttributeValueDto> Attributes { get; init; } = new();
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public string? RowVersion { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<int> EnabledShippingIds { get; init; } = new();
}

public record AttributeValueDto(
    int Id,
    string TypeName,
    string TypeDisplayName,
    string Value,
    string DisplayValue,
    string? HexCode);

public record AttributeTypeWithValuesDto(
    int Id,
    string Name,
    string DisplayName,
    int SortOrder,
    bool IsActive,
    List<AttributeValueSimpleDto> Values);

public record AttributeValueSimpleDto(
    int Id,
    string Value,
    string DisplayValue,
    string? HexCode,
    int SortOrder,
    bool IsActive);



public record CreateProductVariantInput
{
    public int? Id { get; init; }
    public string? Sku { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsActive { get; init; } = true;
    public List<int> AttributeValueIds { get; init; } = new();
    public decimal ShippingMultiplier { get; init; } = 1;
    public List<int>? EnabledShippingIds { get; init; }
}

/// <summary>
/// DTO قدیمی ادمین - برای Handlerهای Legacy که مستقیماً از Repository بارگذاری می‌کنند.
/// در آینده به AdminProductDetailDto مهاجرت داده شود.
/// </summary>
public record AdminProductViewDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int BrandId { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public string? IconUrl { get; init; }
    public IEnumerable<Application.Media.Features.Shared.MediaDto> Images { get; init; } = [];
    public List<ProductVariantResponseDto> Variants { get; init; } = [];
    public string? RowVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public BrandSummaryInProductDto Brand { get; init; } = null!;
}

/// <summary>
/// DTO عمومی محصول - برای Handlerهای Legacy که مستقیماً از Repository بارگذاری می‌کنند.
/// در آینده به PublicProductDetailDto مهاجرت داده شود.
/// </summary>
public record PublicProductViewDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int BrandId { get; init; }
    public string? IconUrl { get; init; }
    public IEnumerable<Application.Media.Features.Shared.MediaDto> Images { get; init; } = [];
    public List<ProductVariantResponseDto> Variants { get; init; } = [];
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public int TotalStock { get; init; }
    public BrandSummaryInProductDto Brand { get; init; } = null!;
}

public record ProductVariantResponseDto
{
    public int Id { get; init; }
    public string? Sku { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsActive { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<int> EnabledShippingIds { get; init; } = [];
    public Dictionary<string, AttributeValueDto> Attributes { get; init; } = new();
    public IEnumerable<Application.Media.Features.Shared.MediaDto> Images { get; init; } = [];
    public string? RowVersion { get; init; }
    public bool IsInStock { get; init; }
    public bool HasDiscount { get; init; }
    public int DiscountPercentage { get; init; }
}

/// <summary>
/// DTO واریانت ورودی - برای UpdateProductHandler
/// </summary>
public record CreateProductVariantDto
{
    public int? Id { get; init; }
    public string? Sku { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public int? LowStockThreshold { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal ShippingMultiplier { get; init; } = 1;
    public List<int> AttributeValueIds { get; init; } = [];
    public List<int>? EnabledShippingIds { get; init; }
}

public record ProductDto
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

public record AdminProductListDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int BrandId { get; init; }
    public string? BrandName { get; init; }
    public string? CategoryName { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public int TotalStock { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int SalesCount { get; init; }
    public int VariantCount { get; init; }
    public string? IconUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record ProductSearchParams
{
    public string? SearchTerm { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? InStock { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record CreateProductInput(
    string Name,
    string Slug,
    int CategoryId,
    int BrandId,
    string? Description
);

public record UpdateProductInput(
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

public record BrandSummaryInProductDto(int Id, string Name, string CategoryName);