namespace Domain.Variant.Entities;

/// <summary>
/// موجودیت واسط برای فعال/غیرفعال کردن روش‌های ارسال خاص برای یک واریانت
/// </summary>
public class ProductVariantShippingMethod : BaseEntity
{
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int ShippingMethodId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}