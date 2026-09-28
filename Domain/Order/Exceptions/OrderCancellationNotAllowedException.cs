using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Order.Exceptions;

public sealed class OrderCancellationNotAllowedException(OrderStatusValue currentStatus)
    : ConflictException<OrderStatusValue>(
        "ORDER_CANCELLATION_NOT_ALLOWED",
        currentStatus,
        $"Order in status '{currentStatus.DisplayName}' cannot be cancelled.")
{
    public OrderStatusValue CurrentStatus => Value;
}
