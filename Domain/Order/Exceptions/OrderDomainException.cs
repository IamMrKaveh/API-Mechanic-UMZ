using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class InvalidOrderTransitionException(OrderStatusValue fromStatus, OrderStatusValue toStatus) : DomainException($"Cannot transition order from '{fromStatus.DisplayName}' to '{toStatus.DisplayName}'.")
{
    public OrderStatusValue FromStatus { get; } = fromStatus;
    public OrderStatusValue ToStatus { get; } = toStatus;

    public override string ErrorCode => "INVALID_ORDER_TRANSITION";
}

public sealed class EmptyOrderException : DomainException
{
    public override string ErrorCode => "EMPTY_ORDER";

    public EmptyOrderException()
        : base("An order must contain at least one item.")
    {
    }
}

public sealed class OrderCancellationNotAllowedException(OrderStatusValue currentStatus) : DomainException($"Order in status '{currentStatus.DisplayName}' cannot be cancelled.")
{
    public OrderStatusValue CurrentStatus { get; } = currentStatus;

    public override string ErrorCode => "ORDER_CANCELLATION_NOT_ALLOWED";
}