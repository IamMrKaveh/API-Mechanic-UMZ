namespace Domain.Order.Exceptions;

public sealed class OrderAlreadyPaidException : DomainException
{
    public int OrderId { get; }
    public DateTime PaymentDate { get; }

    public OrderAlreadyPaidException(int orderId, DateTime paymentDate)
        : base($"سفارش {orderId} قبلاً در تاریخ {paymentDate:yyyy/MM/dd HH:mm} پرداخت شده است.")
    {
        OrderId = orderId;
        PaymentDate = paymentDate;
    }
}