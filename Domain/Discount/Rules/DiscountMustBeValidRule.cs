using Domain.Discount.Aggregates;

namespace Domain.Discount.Rules;

public sealed class DiscountMustBeValidRule(DiscountCode discountCode, Money orderAmount) : IBusinessRule
{
    private readonly DiscountCode _discountCode = discountCode;
    private readonly Money _orderAmount = orderAmount;

    public string Message => _discountCode.ValidateForApplication(_orderAmount).FailureReason
                             ?? "کد تخفیف نامعتبر است.";

    public bool IsBroken()
    {
        var validation = _discountCode.ValidateForApplication(_orderAmount);
        return !validation.IsValid;
    }
}