namespace Domain.Inventory.Events;

public sealed class StockReturnedEvent(int variantId, int orderId, int quantity) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int OrderId { get; } = orderId;
    public int Quantity { get; } = quantity;
}