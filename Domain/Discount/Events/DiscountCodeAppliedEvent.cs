using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCodeAppliedEvent(
    DiscountCodeId discountCodeId,
    string code,
    UserId userId,
    OrderId orderId,
    decimal discountedAmount,
    int usageCount) : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; } = discountCodeId;
    public string Code { get; } = code;
    public UserId UserId { get; } = userId;
    public OrderId OrderId { get; } = orderId;
    public decimal DiscountedAmount { get; } = discountedAmount;
    public int UsageCount { get; } = usageCount;
}