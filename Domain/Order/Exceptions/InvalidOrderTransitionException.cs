using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class InvalidOrderTransitionException(OrderStatusValue fromStatus, OrderStatusValue toStatus) : DomainException($"Cannot transition order from '{fromStatus.DisplayName}' to '{toStatus.DisplayName}'.")
{
    public OrderStatusValue FromStatus { get; } = fromStatus;
    public OrderStatusValue ToStatus { get; } = toStatus;

    public override string ErrorCode => "INVALID_ORDER_TRANSITION";
}