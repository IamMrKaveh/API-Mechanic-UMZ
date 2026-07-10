using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class OrderNotFoundException : DomainException
{
    public OrderId? OrderId { get; }

    public override string ErrorCode => "ORDER_NOT_FOUND";

    public OrderNotFoundException()
        : base("سفارش یافت نشد.")
    {
    }

    public OrderNotFoundException(OrderId orderId)
        : base($"سفارش با شناسه '{orderId.Value}' یافت نشد.")
    {
        OrderId = orderId;
    }
}