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

public record ProductVariantViewDto
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsActive { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();
}

public record CreateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public Guid BrandId { get; init; }
}

public record UpdateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public Guid BrandId { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
}

public sealed record VariantPriceUpdateInput(
    Guid ProductId,
    Guid VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice);