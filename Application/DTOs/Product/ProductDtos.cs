namespace Application.DTOs.Product;

public enum ProductSortOptions { Newest, Oldest, PriceAsc, PriceDesc, NameAsc, NameDesc, DiscountDesc, DiscountAsc }

public class ProductSearchDto { public string? Name { get; set; } public int? CategoryId { get; set; } public decimal? MinPrice { get; set; } public decimal? MaxPrice { get; set; } public bool? InStock { get; set; } public bool? HasDiscount { get; set; } public bool? IsUnlimited { get; set; } public bool? IncludeInactive { get; set; } public bool? IncludeDeleted { get; set; } public ProductSortOptions SortBy { get; set; } = ProductSortOptions.Newest; public int Page { get; set; } = 1; public int PageSize { get; set; } = 10; public int? CategoryGroupId { get; set; } }

public class ProductDto { public int? Id { get; set; } public required string Name { get; set; } public string? Description { get; set; } public int CategoryGroupId { get; set; } public bool IsActive { get; set; } = true; public string? Sku { get; set; } public string? RowVersion { get; set; } public string VariantsJson { get; set; } = "[]"; public List<FileDto>? Images { get; set; } public int? PrimaryImageIndex { get; set; } public List<int>? DeletedMediaIds { get; set; } }

public class CreateProductVariantDto { public int? Id { get; set; } public string? Sku { get; set; } public decimal PurchasePrice { get; set; } public decimal SellingPrice { get; set; } public decimal OriginalPrice { get; set; } public int Stock { get; set; } public bool IsUnlimited { get; set; } public bool IsActive { get; set; } = true; public List<int> AttributeValueIds { get; set; } = []; public decimal ShippingMultiplier { get; set; } = 1; public List<int> EnabledShippingMethodIds { get; set; } = []; }

public record AttributeValueDto(int Id, string TypeName, string TypeDisplayName, string Value, string DisplayValue, string? HexCode);

public class ProductVariantResponseDto { public int Id { get; set; } public string? Sku { get; set; } public decimal PurchasePrice { get; set; } public decimal OriginalPrice { get; set; } public decimal SellingPrice { get; set; } public int Stock { get; set; } public bool IsUnlimited { get; set; } public bool IsActive { get; set; } public bool IsInStock { get; set; } public bool HasDiscount { get; set; } public decimal DiscountPercentage { get; set; } public Dictionary<string, AttributeValueDto> Attributes { get; set; } = []; public IEnumerable<MediaDto> Images { get; set; } = []; public string? RowVersion { get; set; } public decimal ShippingMultiplier { get; set; } public List<int> EnabledShippingMethodIds { get; set; } = []; }

public class PublicProductViewDto { public int Id { get; set; } public required string Name { get; set; } public string? Description { get; set; } public string? Sku { get; set; } public bool IsActive { get; set; } public int CategoryGroupId { get; set; } public object? CategoryGroup { get; set; } public IEnumerable<ProductVariantResponseDto> Variants { get; set; } = []; public string? IconUrl { get; set; } public IEnumerable<MediaDto> Images { get; set; } = []; public decimal MinPrice { get; set; } public decimal MaxPrice { get; set; } public int TotalStock { get; set; } public bool HasMultipleVariants { get; set; } }

public class AdminProductViewDto : PublicProductViewDto { public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } public bool IsDeleted { get; set; } public required string RowVersion { get; set; } }

public class AdminProductListDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string? Sku { get; set; } public bool IsActive { get; set; } public bool IsDeleted { get; set; } public string CategoryName { get; set; } = string.Empty; public string CategoryGroupName { get; set; } = string.Empty; public string? IconUrl { get; set; } public int TotalStock { get; set; } public int VariantCount { get; set; } public decimal MinPrice { get; set; } public decimal MaxPrice { get; set; } public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } }

public class ProductStockDto { public int? VariantId { get; set; } public int Quantity { get; set; } public string? Notes { get; set; } }

public record SetDiscountDto(decimal OriginalPrice, decimal DiscountedPrice);

public class ProductSummaryDto { public int Id { get; set; } public required string Name { get; set; } public string? Icon { get; set; } public int Count { get; set; } public decimal SellingPrice { get; set; } public decimal PurchasePrice { get; set; } public bool IsInStock { get; set; } }

public class AttributeTypeWithValuesDto { public int Id { get; set; } public required string Name { get; set; } public required string DisplayName { get; set; } public required List<AttributeValueSimpleDto> Values { get; set; } }

public class AttributeValueSimpleDto { public int Id { get; set; } public required string Value { get; set; } public required string DisplayValue { get; set; } public string? HexCode { get; set; } }

public class StockAdjustmentDto { public int VariantId { get; set; } public required string TransactionType { get; set; } public int QuantityChange { get; set; } public string? Notes { get; set; } public string? ReferenceNumber { get; set; } }