using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public class OutOfStockEvent(VariantId variantId, ProductId productId, ProductName productName) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public ProductName ProductName { get; } = productName;
    public DateTime OutOfStockAt { get; } = DateTime.UtcNow;
}