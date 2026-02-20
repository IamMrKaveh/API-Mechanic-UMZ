namespace Domain.Variant.Rules;

public class VariantMustHaveValidPricingRule : IBusinessRule
{
    private readonly decimal _purchasePrice;
    private readonly decimal _sellingPrice;
    private readonly decimal _originalPrice;

    public VariantMustHaveValidPricingRule(decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        _purchasePrice = purchasePrice;
        _sellingPrice = sellingPrice;
        _originalPrice = originalPrice;
    }

    public bool IsBroken()
    {
        if (_sellingPrice < _purchasePrice) return true; // فروش زیر قیمت خرید (Loss violation)
        if (_originalPrice > 0 && _sellingPrice > _originalPrice) return true; // قیمت فروش بیشتر از قیمت اصلی
        return false;
    }

    public string Message => "قیمت‌گذاری محصول معتبر نیست (قیمت فروش نباید کمتر از خرید یا بیشتر از قیمت خط‌خورده باشد).";
}