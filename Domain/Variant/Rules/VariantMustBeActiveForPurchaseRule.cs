namespace Domain.Variant.Rules;

public sealed class VariantMustBeActiveForPurchaseRule : IBusinessRule
{
    private readonly ProductVariant _variant;
    private readonly Product.Product _product;

    public VariantMustBeActiveForPurchaseRule(ProductVariant variant, Product.Product product)
    {
        _variant = variant;
        _product = product;
    }

    public bool IsBroken()
    {
        return _product.IsDeleted || !_product.IsActive || _variant.IsDeleted || !_variant.IsActive;
    }

    public string Message => "محصول یا واریانت غیرفعال است و قابل خرید نیست.";
}