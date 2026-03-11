namespace Domain.Variant.Events;

/// <summary>
/// رویداد تغییر موجودی واریانت - self-contained برای Cache و Search update بدون نیاز به DB query
/// </summary>
public sealed class VariantStockChangedEvent(
    int variantId,
    int productId,
    int quantityChanged) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public int QuantityChanged { get; } = quantityChanged;
}