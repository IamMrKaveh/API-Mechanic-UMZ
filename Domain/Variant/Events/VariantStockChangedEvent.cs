namespace Domain.Variant.Events;

/// <summary>
/// رویداد تغییر موجودی واریانت - self-contained برای Cache و Search update بدون نیاز به DB query
/// </summary>
public sealed class VariantStockChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int QuantityChanged { get; }

    public VariantStockChangedEvent(
        int variantId,
        int productId,
        int quantityChanged)
    {
        VariantId = variantId;
        ProductId = productId;
        QuantityChanged = quantityChanged;
    }
}