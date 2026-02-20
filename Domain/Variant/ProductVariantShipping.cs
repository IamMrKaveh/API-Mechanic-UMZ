namespace Domain.Variant;

/// <summary>
/// موجودیت واسط برای فعال/غیرفعال کردن روش‌های ارسال خاص برای یک واریانت
/// </summary>
public class ProductVariantShipping : BaseEntity
{
    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int ShippingId { get; set; }
    public Shipping.Shipping Shipping { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}