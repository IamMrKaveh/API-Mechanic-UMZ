namespace Domain.Variant.Rules;

public sealed class DiscountCannotExceedOriginalPriceRule : IBusinessRule
{
    private readonly decimal _originalPrice;
    private readonly decimal _discountedPrice;
    private readonly decimal _purchasePrice;

    public DiscountCannotExceedOriginalPriceRule(decimal originalPrice, decimal discountedPrice, decimal purchasePrice)
    {
        _originalPrice = originalPrice;
        _discountedPrice = discountedPrice;
        _purchasePrice = purchasePrice;
    }

    public bool IsBroken()
    {
        if (_discountedPrice > _originalPrice)
            return true;

        if (_discountedPrice < _purchasePrice)
            return true;

        return false;
    }

    public string Message => "قیمت تخفیف‌خورده نمی‌تواند بیشتر از قیمت اصلی یا کمتر از قیمت خرید باشد.";
}