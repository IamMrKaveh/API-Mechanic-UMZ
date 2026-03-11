namespace Domain.Discount.Exceptions;

public sealed class MinimumOrderAmountNotMetException(string code, decimal minAmount, decimal orderAmount) : DomainException($"حداقل مبلغ سفارش برای کد تخفیف '{code}' مبلغ {minAmount:N0} تومان است. مبلغ سفارش شما: {orderAmount:N0} تومان")
{
    public string Code { get; } = code;
    public decimal MinAmount { get; } = minAmount;
    public decimal OrderAmount { get; } = orderAmount;
}