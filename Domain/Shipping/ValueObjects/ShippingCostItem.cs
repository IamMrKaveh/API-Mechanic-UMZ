namespace Domain.Shipping.ValueObjects;

public sealed record ShippingCostItem(
    Guid VariantId,
    decimal ShippingMultiplier,
    int Quantity)
{
    public static ShippingCostItem Create(Guid variantId, decimal shippingMultiplier, int quantity)
    {
        if (variantId == Guid.Empty)
            throw new DomainException("شناسه واریانت الزامی است.");

        if (shippingMultiplier < 0)
            throw new DomainException("ضریب ارسال نمی‌تواند منفی باشد.");

        if (quantity <= 0)
            throw new DomainException("تعداد باید بزرگتر از صفر باشد.");

        return new ShippingCostItem(variantId, shippingMultiplier, quantity);
    }
}