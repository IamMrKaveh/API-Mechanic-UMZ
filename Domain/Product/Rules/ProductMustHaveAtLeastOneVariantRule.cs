namespace Domain.Product.Rules;

public sealed class ProductMustHaveAtLeastOneVariantRule : IBusinessRule
{
    private readonly IReadOnlyCollection<ProductVariant> _variants;

    public ProductMustHaveAtLeastOneVariantRule(IReadOnlyCollection<ProductVariant> variants)
    {
        _variants = variants;
    }

    public bool IsBroken() => !_variants.Any(v => !v.IsDeleted);

    public string Message => "محصول باید حداقل یک واریانت فعال داشته باشد.";
}