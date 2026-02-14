namespace Domain.Product.Rules;

public sealed class VariantMustBeActiveForPurchaseRule : IBusinessRule
{
    private readonly ProductVariant _variant;
    private readonly Product _product;

    public VariantMustBeActiveForPurchaseRule(ProductVariant variant, Product product)
    {
        _variant = variant;
        _product = product;
    }

    public bool IsBroken()
    {
        if (_product.IsDeleted || !_product.IsActive)
            return true;

        if (_variant.IsDeleted || !_variant.IsActive)
            return true;

        return false;
    }

    public string Message => "محصول یا واریانت غیرفعال است و قابل خرید نیست.";
}