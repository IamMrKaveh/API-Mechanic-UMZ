using Domain.Variant.ValueObjects;

namespace Domain.Shipping.ValueObjects;

public sealed record ShippingCostItem(
    VariantId VariantId,
    decimal ShippingMultiplier,
    int Quantity)
{
}