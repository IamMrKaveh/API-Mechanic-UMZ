using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class OrderCancellationNotAllowedException(OrderStatusValue currentStatus) : DomainException($"Order in status '{currentStatus.DisplayName}' cannot be cancelled.")
{
    public OrderStatusValue CurrentStatus { get; } = currentStatus;

    public override string ErrorCode => "ORDER_CANCELLATION_NOT_ALLOWED";
}