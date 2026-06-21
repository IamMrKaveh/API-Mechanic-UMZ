using Application.Attribute.Features.Shared;
using Application.Media.Features.Shared;

namespace Application.Variant.Features.Shared;

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
    public decimal OriginalPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public int Stock { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsActive { get; init; }
    public bool IsInStock { get; init; }
    public bool HasDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public int StockQuantity { get; init; }
    public Dictionary<string, AttributeValueDto> Attributes { get; init; } = [];
    public IEnumerable<MediaDto> Images { get; init; } = [];
    public string? RowVersion { get; init; }
    public decimal ShippingMultiplier { get; init; }
    public List<Guid> EnabledShippingIds { get; init; } = [];
}