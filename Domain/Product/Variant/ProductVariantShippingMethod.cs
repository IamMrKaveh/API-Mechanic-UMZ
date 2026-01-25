namespace Domain.Product.Variant;

public class ProductVariantShippingMethod
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int ShippingMethodId { get; set; }
    public Order.ShippingMethod ShippingMethod { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
