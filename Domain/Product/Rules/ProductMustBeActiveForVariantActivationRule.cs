namespace Domain.Product.Rules;

public sealed class ProductMustBeActiveForVariantActivationRule : IBusinessRule
{
    private readonly Aggregates.Product _product;

    public ProductMustBeActiveForVariantActivationRule(Aggregates.Product product)
    {
        _product = product;
    }

    public bool IsBroken()
    {
        return !_product.IsActive;
    }

    public string Message => "امکان فعال‌سازی واریانت در محصول غیرفعال وجود ندارد.";
}