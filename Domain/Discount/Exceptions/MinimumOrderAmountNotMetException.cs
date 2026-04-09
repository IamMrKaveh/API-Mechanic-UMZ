namespace Domain.Discount.Exceptions;

public sealed class MinimumOrderAmountNotMetException : DomainException
{
    public string Code { get; }
    public decimal MinAmount { get; }
    public decimal OrderAmount { get; }

    public override string ErrorCode => "MINIMUM_ORDER_AMOUNT_NOT_MET";

    public MinimumOrderAmountNotMetException(string code, decimal minAmount, decimal orderAmount)
        : base($"حداقل مبلغ سفارش برای کد تخفیف '{code}' مبلغ {minAmount:N0} تومان است. مبلغ سفارش شما: {orderAmount:N0} تومان")
    {
        Code = code;
        MinAmount = minAmount;
        OrderAmount = orderAmount;
    }
}