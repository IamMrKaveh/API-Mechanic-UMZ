using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class OrderAlreadyPaidException : DomainException
{
    public OrderId OrderId { get; }

    public override string ErrorCode => "ORDER_ALREADY_PAID";

    public OrderAlreadyPaidException(OrderId orderId)
        : base("سفارش قبلاً پرداخت شده است.")
    {
        OrderId = orderId;
    }
}