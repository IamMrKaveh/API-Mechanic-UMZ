namespace Domain.Order.Rules;

public class OrderCannotExceedMaximumAmountRule(decimal orderTotal, decimal maxAmount = 500_000_000) : IBusinessRule
{
    private readonly decimal _orderTotal = orderTotal;
    private readonly decimal _maxAmount = maxAmount;

    public bool IsBroken() => _orderTotal > _maxAmount;

    public string Message => $"مبلغ سفارش نمی‌تواند بیشتر از {_maxAmount:N0} تومان باشد.";
}