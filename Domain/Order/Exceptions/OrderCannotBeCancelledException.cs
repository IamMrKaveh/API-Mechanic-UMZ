namespace Domain.Order.Exceptions;

public sealed class OrderCannotBeCancelledException : DomainException
{
    public int OrderId { get; }
    public string CurrentStatus { get; }
    public string Reason { get; }

    public OrderCannotBeCancelledException(int orderId, string currentStatus, string reason)
        : base($"سفارش {orderId} با وضعیت {currentStatus} قابل لغو نیست. {reason}")
    {
        OrderId = orderId;
        CurrentStatus = currentStatus;
        Reason = reason;
    }
}