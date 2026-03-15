using Domain.Variant.Aggregates;

namespace Domain.Variant.Rules;

public sealed class VariantMustBeActiveForPurchaseRule(ProductVariant variant) : IBusinessRule
{
    private readonly ProductVariant _variant = variant;

    public bool IsBroken()
    {
        return _variant.IsDeleted || !_variant.IsActive;
    }

    public string Message => "واریانت غیرفعال است و قابل خرید نیست.";
}