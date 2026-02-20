namespace Domain.Inventory;

/// <summary>
/// موجودی یک Variant در یک انبار مشخص.
/// </summary>
public sealed class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; private set; }
    public int VariantId { get; private set; }
    public int OnHand { get; private set; }  // موجودی فیزیکی
    public int Reserved { get; private set; }  // رزرو شده
    public int Damaged { get; private set; }  // معیوب
    public int Available => OnHand - Reserved;  // موجودی قابل فروش

    // Navigation
    public Warehouse? Warehouse { get; private set; }

    public ProductVariant? Variant { get; private set; }

    private WarehouseStock()
    { }

    public static WarehouseStock Create(int warehouseId, int variantId) =>
        new() { WarehouseId = warehouseId, VariantId = variantId };

    public void AddStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("تعداد باید مثبت باشد.");
        OnHand += quantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0) throw new DomainException("تعداد باید مثبت باشد.");
        if (Available < quantity)
            throw new DomainException($"موجودی کافی نیست. موجودی قابل دسترس: {Available}");
        Reserved += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0) throw new DomainException("تعداد باید مثبت باشد.");
        Reserved = Math.Max(0, Reserved - quantity);
    }

    public void CommitReservation(int quantity)
    {
        if (quantity <= 0) throw new DomainException("تعداد باید مثبت باشد.");
        Reserved = Math.Max(0, Reserved - quantity);
        OnHand = Math.Max(0, OnHand - quantity);
    }

    public void MarkAsDamaged(int quantity)
    {
        if (quantity <= 0) throw new DomainException("تعداد باید مثبت باشد.");
        if (OnHand < quantity) throw new DomainException("موجودی کافی نیست.");
        OnHand -= quantity;
        Damaged += quantity;
    }
}