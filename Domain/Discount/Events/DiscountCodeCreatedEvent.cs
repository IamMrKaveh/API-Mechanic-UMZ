using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCodeCreatedEvent(
    DiscountCodeId discountCodeId,
    string code,
    DiscountType type,
    decimal value,
    int? usageLimit,
    DateTime? expiresAt) : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; } = discountCodeId;
    public string Code { get; } = code;
    public DiscountType Type { get; } = type;
    public decimal Value { get; } = value;
    public int? UsageLimit { get; } = usageLimit;
    public DateTime? ExpiresAt { get; } = expiresAt;
}