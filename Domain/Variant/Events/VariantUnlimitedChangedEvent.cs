namespace Domain.Variant.Events;

/// <summary>
/// رویداد تغییر وضعیت موجودی نامحدود واریانت
/// مورد استفاده برای cache invalidation و search index sync
/// </summary>
public sealed class VariantUnlimitedChangedEvent(int variantId, int productId, bool isUnlimited) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public bool IsUnlimited { get; } = isUnlimited;
}