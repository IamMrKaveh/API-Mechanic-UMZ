using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountUsageRecordedEvent(
    DiscountUsageId usageId,
    DiscountCodeId discountCodeId,
    UserId userId,
    OrderId orderId,
    decimal discountedAmount) : DomainEvent
{
    public DiscountUsageId UsageId { get; } = usageId;
    public DiscountCodeId DiscountCodeId { get; } = discountCodeId;
    public UserId UserId { get; } = userId;
    public OrderId OrderId { get; } = orderId;
    public decimal DiscountedAmount { get; } = discountedAmount;
}