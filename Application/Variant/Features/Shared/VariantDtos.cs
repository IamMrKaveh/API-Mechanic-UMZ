using Application.Attribute.Features.Shared;
using Application.Media.Features.Shared;

namespace Application.Variant.Features.Shared;

public record ProductVariantDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal FinalPrice { get; init; }
    public int StockQuantity { get; init; }
}

public record VariantStockChangedApplicationNotification : INotification
{
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public int QuantityChanged { get; init; }
    public int NewOnHand { get; init; }
    public int NewReserved { get; init; }
    public int NewAvailable { get; init; }
    public bool IsInStock { get; init; }
}

public sealed record ProductVariantViewDto
{
    public Guid Id { get; init; }
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

public sealed record CreateProductVariantInput(
    Guid? Id,
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

public sealed record ProductVariantResponseDto(
    Guid Id,
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
    IEnumerable<MediaDto> Images,
    string? RowVersion,
    bool IsInStock,
    bool HasDiscount,
    int DiscountPercentage
);

public sealed record CreateProductVariantDto(
    Guid? Id,
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

public sealed record AddVariantDto(
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock = 0,
    bool IsUnlimited = false,
    decimal ShippingMultiplier = 1,
    ICollection<Guid>? AttributeValueIds = null,
    ICollection<Guid>? EnabledShippingIds = null
);

public sealed record UpdateVariantDto(
    string? Sku,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice,
    int Stock = 0,
    bool IsUnlimited = false,
    decimal ShippingMultiplier = 1,
    ICollection<Guid>? AttributeValueIds = null,
    ICollection<Guid>? EnabledShippingIds = null
);

public sealed record AddStockDto(
    int Quantity,
    string Notes
);

public sealed record RemoveStockDto(
    int Quantity,
    string Notes
);

public sealed record UpdateVariantShippingDto(
    decimal ShippingMultiplier,
    ICollection<Guid> EnabledShippingIds
);