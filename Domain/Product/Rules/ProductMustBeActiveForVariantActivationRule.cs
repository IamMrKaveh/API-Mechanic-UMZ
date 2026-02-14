namespace Domain.Product.Rules;

public sealed class ProductMustBeActiveForVariantActivationRule : IBusinessRule
{
    private readonly Product _product;

    public ProductMustBeActiveForVariantActivationRule(Product product)
    {
        _product = product;
    }

    public bool IsBroken()
    {
        return _product.IsDeleted || !_product.IsActive;
    }

    public string Message => "امکان فعال‌سازی واریانت در محصول غیرفعال یا حذف‌شده وجود ندارد.";
}