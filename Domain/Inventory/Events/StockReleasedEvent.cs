using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public class StockReleasedEvent(VariantId variantId, ProductId productId, int quantity, string? reason = null) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public int Quantity { get; } = quantity;
    public string? Reason { get; } = reason;
}