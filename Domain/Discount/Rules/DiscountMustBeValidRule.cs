namespace Domain.Discount.Rules;

public sealed class DiscountMustBeValidRule : IBusinessRule
{
    private readonly DiscountCode _discount;
    private readonly decimal _orderTotal;
    private readonly int _userPreviousUsageCount;

    public DiscountMustBeValidRule(DiscountCode discount, decimal orderTotal, int userPreviousUsageCount = 0)
    {
        _discount = discount;
        _orderTotal = orderTotal;
        _userPreviousUsageCount = userPreviousUsageCount;
    }

    public bool IsBroken()
    {
        var validation = _discount.ValidateForApplication(_orderTotal, 0, _userPreviousUsageCount);
        return !validation.IsValid;
    }

    public string Message => "کد تخفیف معتبر نیست.";
}