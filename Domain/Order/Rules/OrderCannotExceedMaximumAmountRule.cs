namespace Domain.Order.Rules;

public class OrderCannotExceedMaximumAmountRule : IBusinessRule
{
    private readonly decimal _orderTotal;
    private readonly decimal _maxAmount;

    public OrderCannotExceedMaximumAmountRule(decimal orderTotal, decimal maxAmount = 500_000_000)
    {
        _orderTotal = orderTotal;
        _maxAmount = maxAmount;
    }

    public bool IsBroken() => _orderTotal > _maxAmount;

    public string Message => $"مبلغ سفارش نمی‌تواند بیشتر از {_maxAmount:N0} تومان باشد.";
}