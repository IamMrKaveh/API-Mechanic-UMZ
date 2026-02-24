namespace Domain.Variant.Events;

/// <summary>
/// رویداد تغییر وضعیت موجودی نامحدود واریانت
/// مورد استفاده برای cache invalidation و search index sync
/// </summary>
public sealed class VariantUnlimitedChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public bool IsUnlimited { get; }

    public VariantUnlimitedChangedEvent(int variantId, int productId, bool isUnlimited)
    {
        VariantId = variantId;
        ProductId = productId;
        IsUnlimited = isUnlimited;
    }
}