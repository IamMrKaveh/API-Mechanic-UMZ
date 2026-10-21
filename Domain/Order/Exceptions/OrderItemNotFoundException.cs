using Domain.Order.ValueObjects;

namespace Domain.Order.Exceptions;

public sealed class OrderItemNotFoundException(OrderItemId orderItemId) : DomainException($"Order item '{orderItemId.Value}' was not found in this order.")
{
    public Guid OrderItemId { get; } = orderItemId.Value;

    public override string ErrorCode => "ORDER_ITEM_NOT_FOUND";
}
