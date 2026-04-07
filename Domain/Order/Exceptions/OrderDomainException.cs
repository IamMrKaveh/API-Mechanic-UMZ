using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class OrderNotFoundException : DomainException
{
    public OrderId OrderId { get; }

    public override string ErrorCode => "ORDER_NOT_FOUND";

    public OrderNotFoundException(OrderId orderId)
        : base($"Order with ID '{orderId}' was not found.")
    {
        OrderId = orderId;
    }
}

public sealed class InvalidOrderTransitionException : DomainException
{
    public OrderStatusValue FromStatus { get; }
    public OrderStatusValue ToStatus { get; }

    public override string ErrorCode => "INVALID_ORDER_TRANSITION";

    public InvalidOrderTransitionException(OrderStatusValue fromStatus, OrderStatusValue toStatus)
        : base($"Cannot transition order from '{fromStatus.DisplayName}' to '{toStatus.DisplayName}'.")
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}

public sealed class EmptyOrderException : DomainException
{
    public override string ErrorCode => "EMPTY_ORDER";

    public EmptyOrderException()
        : base("An order must contain at least one item.")
    {
    }
}

public sealed class OrderAlreadyCancelledException : DomainException
{
    public OrderId OrderId { get; }

    public override string ErrorCode => "ORDER_ALREADY_CANCELLED";

    public OrderAlreadyCancelledException(OrderId orderId)
        : base($"Order '{orderId}' has already been cancelled.")
    {
        OrderId = orderId;
    }
}

public sealed class OrderAlreadyPaidException : DomainException
{
    public OrderId OrderId { get; }

    public override string ErrorCode => "ORDER_ALREADY_PAID";

    public OrderAlreadyPaidException(OrderId orderId)
        : base($"Order '{orderId}' has already been paid.")
    {
        OrderId = orderId;
    }
}

public sealed class OrderCancellationNotAllowedException : DomainException
{
    public OrderStatusValue CurrentStatus { get; }

    public override string ErrorCode => "ORDER_CANCELLATION_NOT_ALLOWED";

    public OrderCancellationNotAllowedException(OrderStatusValue currentStatus)
        : base($"Order in status '{currentStatus.DisplayName}' cannot be cancelled.")
    {
        CurrentStatus = currentStatus;
    }
}