using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountAppliedEvent(
    DiscountCodeId discountId,
    UserId userId,
    OrderId orderId) : DomainEvent
{
    public DiscountCodeId DiscountId { get; } = discountId;
    public UserId UserId { get; } = userId;
    public OrderId OrderId { get; } = orderId;
}