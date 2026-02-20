namespace Domain.Variant.Events;

/// <summary>
/// رویداد تغییر موجودی واریانت - self-contained برای Cache و Search update بدون نیاز به DB query
/// </summary>
public sealed class VariantStockChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int QuantityChange { get; }

    // اطلاعات کامل مقادیر جدید - برای Cache و Search بدون رفت به DB
    public int NewOnHand { get; }

    public int NewReserved { get; }
    public int NewAvailable { get; }
    public bool IsInStock { get; }

    public VariantStockChangedEvent(
        int variantId,
        int productId,
        int quantityChange,
        int newOnHand = 0,
        int newReserved = 0,
        int newAvailable = 0,
        bool isInStock = false)
    {
        VariantId = variantId;
        ProductId = productId;
        QuantityChange = quantityChange;
        NewOnHand = newOnHand;
        NewReserved = newReserved;
        NewAvailable = newAvailable;
        IsInStock = isInStock;
    }
}