using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountUsageCancelledEvent(DiscountCodeId discountId, OrderId orderId) : DomainEvent
{
    public DiscountCodeId DiscountId { get; } = discountId;
    public OrderId OrderId { get; } = orderId;
}