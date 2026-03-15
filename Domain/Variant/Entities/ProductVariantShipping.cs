using Domain.Shipping.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Entities;

public sealed class ProductVariantShipping : Entity<ProductVariantShippingId>
{
    private ProductVariantShipping()
    { }

    public ProductVariantId VariantId { get; private set; } = default!;
    public ShippingId ShippingId { get; private set; } = default!;
    public decimal Weight { get; private set; }
    public decimal Width { get; private set; }
    public decimal Height { get; private set; }
    public decimal Length { get; private set; }

    internal static ProductVariantShipping Create(
        ProductVariantId variantId,
        ShippingId shippingId,
        decimal weight,
        decimal width,
        decimal height,
        decimal length)
    {
        if (weight < 0)
            throw new DomainException("وزن نمی‌تواند منفی باشد.");
        if (width < 0)
            throw new DomainException("عرض نمی‌تواند منفی باشد.");
        if (height < 0)
            throw new DomainException("ارتفاع نمی‌تواند منفی باشد.");
        if (length < 0)
            throw new DomainException("طول نمی‌تواند منفی باشد.");

        return new ProductVariantShipping
        {
            Id = ProductVariantShippingId.NewId(),
            VariantId = variantId,
            ShippingId = shippingId,
            Weight = weight,
            Width = width,
            Height = height,
            Length = length
        };
    }
}