using Application.Variant.Features.Shared;

namespace Application.Product.Features.Shared;

public record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public List<ProductVariantViewDto> Variants { get; init; } = [];
}

public record AdminProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public string? RowVersion { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public List<ProductVariantViewDto> Variants { get; init; } = [];
}

public record PublicProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public List<ProductVariantViewDto> Variants { get; init; } = [];
}

public record ProductCatalogItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public bool IsFeatured { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool HasStock { get; init; }
    public string? PrimaryImageUrl { get; init; }
}

public sealed record ProductCatalogSearchParams(
    int Page,
    int PageSize,
    string? Search,
    Guid? CategoryId,
    Guid? BrandId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStockOnly,
    string? SortBy);

public record ProductListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public Guid BrandId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsDeleted { get; init; }
    public decimal? MinPrice { get; init; }
    public bool HasStock { get; init; }
    public string? PrimaryImageUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public sealed record VariantPriceUpdateInput(
    Guid ProductId,
    Guid VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice);