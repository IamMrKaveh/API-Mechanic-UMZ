using Domain.Discount.Aggregates;

namespace Domain.Discount.Rules;

public sealed class DiscountMustBeValidRule : IBusinessRule
{
    private readonly DiscountCode _discountCode;
    private readonly decimal _orderAmount;

    public DiscountMustBeValidRule(DiscountCode discountCode, decimal orderAmount)
    {
        _discountCode = discountCode;
        _orderAmount = orderAmount;
    }

    public string Message => _discountCode.ValidateForApplication(_orderAmount).FailureReason
                             ?? "کد تخفیف نامعتبر است.";

    public bool IsBroken()
    {
        var validation = _discountCode.ValidateForApplication(_orderAmount);
        return !validation.IsValid;
    }
}