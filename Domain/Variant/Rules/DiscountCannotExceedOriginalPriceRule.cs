namespace Domain.Variant.Rules;

public sealed class DiscountCannotExceedOriginalPriceRule(Money originalPrice, Money discountedPrice) : IBusinessRule
{
    private readonly Money _originalPrice = originalPrice;
    private readonly Money _discountedPrice = discountedPrice;

    public bool IsBroken()
    {
        if (_discountedPrice.Amount > _originalPrice.Amount)
            return true;

        if (_discountedPrice.Amount < 0)
            return true;

        return false;
    }

    public string Message => "قیمت تخفیف‌خورده نمی‌تواند بیشتر از قیمت اصلی یا منفی باشد.";
}