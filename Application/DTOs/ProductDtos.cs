namespace Application.DTOs;

public enum ProductSortOptions
{
    Newest,
    Oldest,
    PriceAsc,
    PriceDesc,
    NameAsc,
    NameDesc,
    DiscountDesc,
    DiscountAsc
}

public class ProductSearchDto
{
    public string? Name { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public bool? HasDiscount { get; set; }
    public bool? IsUnlimited { get; set; }
    public bool? IncludeInactive { get; set; }
    public bool? IncludeDeleted { get; set; }
    public ProductSortOptions SortBy { get; set; } = ProductSortOptions.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ProductDto
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }
    public string? Description { get; set; }
    [Required]
    public int CategoryGroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Sku { get; set; }
    public List<CreateProductVariantDto> Variants { get; set; } = [];
}

public class CreateProductVariantDto
{
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> AttributeValueIds { get; set; } = [];
}

public record AttributeValueDto(
    int Id,
    string TypeName,
    string TypeDisplayName,
    string Value,
    string DisplayValue,
    string? HexCode
);

public class ProductVariantResponseDto
{
    public int Id { get; set; }
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsActive { get; set; }
    public bool IsInStock { get; set; }
    public bool HasDiscount { get; set; }
    public double DiscountPercentage { get; set; }
    public Dictionary<string, AttributeValueDto> Attributes { get; set; } = [];
    public IEnumerable<MediaDto> Images { get; set; } = [];
}

public class PublicProductViewDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public object? CategoryGroup { get; set; }
    public IEnumerable<ProductVariantResponseDto> Variants { get; set; } = [];
    public IEnumerable<MediaDto> Images { get; set; } = [];
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
}

public class AdminProductViewDto : PublicProductViewDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class ProductStockDto
{
    [Required]
    public int? VariantId { get; set; }

    [Required]
    [Range(1, 100000)]
    public int Quantity { get; set; }
}

public record SetDiscountDto(
    [Required] decimal OriginalPrice,
    [Required] decimal DiscountedPrice
);

public class ProductSummaryDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public int Count { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public bool IsInStock { get; set; }
}