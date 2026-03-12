namespace Domain.Variant.Rules;

public class VariantMustHaveValidPricingRule(Money sellingPrice, Money? compareAtPrice) : IBusinessRule
{
    private readonly Money _sellingPrice = sellingPrice;
    private readonly Money? _compareAtPrice = compareAtPrice;

    public bool IsBroken()
    {
        if (_sellingPrice.Amount <= 0)
            return true;

        if (_compareAtPrice is not null && _compareAtPrice.Amount < _sellingPrice.Amount)
            return true;

        return false;
    }

    public string Message => "قیمت‌گذاری واریانت معتبر نیست (قیمت فروش باید مثبت باشد و قیمت مقایسه‌ای نباید کمتر از قیمت فروش باشد).";
}