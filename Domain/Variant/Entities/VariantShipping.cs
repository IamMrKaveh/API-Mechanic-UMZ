using Domain.Shipping.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Entities;

public sealed class VariantShipping : Entity<VariantShippingId>
{
    private VariantShipping()
    { }

    public Variant.Aggregates.ProductVariant Variant { get; private set; } = default!;
    public VariantId VariantId { get; private set; } = default!;
    public Shipping.Aggregates.Shipping Shipping { get; private set; } = default!;
    public ShippingId ShippingId { get; private set; } = default!;

    public decimal Weight { get; private set; }
    public decimal Width { get; private set; }
    public decimal Height { get; private set; }
    public decimal Length { get; private set; }
    public decimal ShippingMultiplier { get; private set; }

    internal static VariantShipping Create(
        VariantId variantId,
        ShippingId shippingId,
        decimal weight,
        decimal width,
        decimal height,
        decimal length,
        decimal shippingMultiplier)
    {
        ValidateDimensions(weight, width, height, length, shippingMultiplier);

        return new VariantShipping
        {
            Id = VariantShippingId.NewId(),
            VariantId = variantId,
            ShippingId = shippingId,
            Weight = weight,
            Width = width,
            Height = height,
            Length = length,
            ShippingMultiplier = shippingMultiplier
        };
    }

    internal void UpdateDimensions(
        decimal weight,
        decimal width,
        decimal height,
        decimal length,
        decimal shippingMultiplier)
    {
        ValidateDimensions(weight, width, height, length, shippingMultiplier);

        Weight = weight;
        Width = width;
        Height = height;
        Length = length;
        ShippingMultiplier = shippingMultiplier;
    }

    private static void ValidateDimensions(
        decimal weight,
        decimal width,
        decimal height,
        decimal length,
        decimal shippingMultiplier)
    {
        if (weight < 0)
            throw new DomainException("وزن نمی‌تواند منفی باشد.");
        if (width < 0)
            throw new DomainException("عرض نمی‌تواند منفی باشد.");
        if (height < 0)
            throw new DomainException("ارتفاع نمی‌تواند منفی باشد.");
        if (length < 0)
            throw new DomainException("طول نمی‌تواند منفی باشد.");
        if (shippingMultiplier <= 0)
            throw new DomainException("ضریب هزینه ارسال باید بزرگتر از صفر باشد.");
    }
}