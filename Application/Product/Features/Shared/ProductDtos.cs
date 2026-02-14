namespace Application.Product.Features.Shared;

// ========== Admin DTOs ==========

public record AdminProductSearchParams
{
    public string? Name { get; init; }
    public int? CategoryId { get; init; }
    public int? CategoryGroupId { get; init; }
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
    public string CategoryGroupName { get; init; } = string.Empty;
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
    public int CategoryGroupId { get; init; }
    public string? IconUrl { get; init; }
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public IEnumerable<ProductVariantViewDto> Variants { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

// ========== Public DTOs ==========

public record ProductCatalogSearchParams
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int? CategoryGroupId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool InStockOnly { get; init; }
    public string? SortBy { get; init; } // price_asc, price_desc, newest, bestselling
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

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? IconUrl { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public bool IsActive { get; set; }
    public object SellingPrice { get; set; }
    public object PurchasePrice { get; set; }
    public object Icon { get; set; }
    public object Count { get; set; }
    public object IsInStock { get; set; }
}

public record PublicProductDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Sku { get; init; }
    public int CategoryGroupId { get; init; }
    public CategoryGroupInfoDto? CategoryGroup { get; init; }
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

public record CategoryGroupInfoDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

// ========== Variant DTOs ==========

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
    public List<int> EnabledShippingMethodIds { get; init; } = new();
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

// ========== Command Input DTOs ==========

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
    public List<int>? EnabledShippingMethodIds { get; init; }
}

/// <summary>
/// DTO قدیمی ادمین - برای Handlerهای Legacy که مستقیماً از Repository بارگذاری می‌کنند.
/// در آینده به AdminProductDetailDto مهاجرت داده شود.
/// </summary>
public class AdminProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int CategoryGroupId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? IconUrl { get; set; }
    public IEnumerable<MediaDto> Images { get; set; } = [];
    public List<ProductVariantResponseDto> Variants { get; set; } = [];
    public string? RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public object CategoryGroup { get; set; }
}

/// <summary>
/// DTO عمومی محصول - برای Handlerهای Legacy که مستقیماً از Repository بارگذاری می‌کنند.
/// در آینده به PublicProductDetailDto مهاجرت داده شود.
/// </summary>
public class PublicProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int CategoryGroupId { get; set; }
    public string? IconUrl { get; set; }
    public IEnumerable<MediaDto> Images { get; set; } = [];
    public List<ProductVariantResponseDto> Variants { get; set; } = [];
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
    public object CategoryGroup { get; set; }
}

public class ProductVariantResponseDto
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsActive { get; set; }
    public decimal ShippingMultiplier { get; set; }
    public List<int> EnabledShippingMethodIds { get; set; } = [];
    public Dictionary<string, AttributeValueDto> Attributes { get; set; } = new();
    public IEnumerable<MediaDto> Images { get; set; } = [];
    public string? RowVersion { get; set; }
    public object IsInStock { get; set; }
    public object HasDiscount { get; set; }
    public object DiscountPercentage { get; set; }
    public object ProductVariantShippingMethods { get; internal set; }

    internal void UpdateDetails(string? sku, decimal shippingMultiplier)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// DTO واریانت ورودی - برای UpdateProductHandler
/// </summary>
public class CreateProductVariantDto
{
    public int? Id { get; set; }
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal ShippingMultiplier { get; set; } = 1;
    public List<int> AttributeValueIds { get; set; } = [];
    public List<int>? EnabledShippingMethodIds { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int CategoryGroupId { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public string? RowVersion { get; set; }
}

public class AdminProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int CategoryGroupId { get; set; }
    public string? CategoryGroupName { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SalesCount { get; set; }
    public int VariantCount { get; set; }
    public string? IconUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class ProductSearchParams
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? CategoryGroupId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? InStock { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}