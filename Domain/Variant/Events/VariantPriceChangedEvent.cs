namespace Domain.Variant.Events;

public sealed class VariantPriceChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public decimal NewPrice { get; }

    public VariantPriceChangedEvent(int variantId, int productId, decimal newPrice)
    {
        VariantId = variantId;
        ProductId = productId;
        NewPrice = newPrice;
    }
}