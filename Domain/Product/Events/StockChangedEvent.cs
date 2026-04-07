using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Events;

public sealed class StockChangedEvent(VariantId variantId, ProductId productId, int oldStock, int newStock, int quantityChange, string reason) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public int OldStock { get; } = oldStock;
    public int NewStock { get; } = newStock;
    public int QuantityChange { get; } = quantityChange;
    public string Reason { get; } = reason;
}