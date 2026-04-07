using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountUsageConfirmedEvent(
    DiscountUsageId usageId,
    OrderId orderId,
    DiscountCodeId discountId) : DomainEvent
{
    public DiscountUsageId UsageId { get; } = usageId;
    public OrderId OrderId { get; } = orderId;
    public DiscountCodeId DiscountId { get; } = discountId;
}