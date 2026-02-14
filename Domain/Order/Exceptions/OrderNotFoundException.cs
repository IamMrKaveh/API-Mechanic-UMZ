namespace Domain.Order.Exceptions;

public sealed class OrderNotFoundException : DomainException
{
    public int OrderId { get; }

    public OrderNotFoundException(int orderId)
        : base($"سفارش با شناسه {orderId} یافت نشد.")
    {
        OrderId = orderId;
    }
}