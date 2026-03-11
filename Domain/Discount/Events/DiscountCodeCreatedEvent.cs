namespace Domain.Discount.Events;

public sealed class DiscountCodeCreatedEvent : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; }
    public string Code { get; }
    public DiscountType Type { get; }
    public decimal Value { get; }
    public int? UsageLimit { get; }
    public DateTime? ExpiresAt { get; }

    public DiscountCodeCreatedEvent(
        DiscountCodeId discountCodeId,
        string code,
        DiscountType type,
        decimal value,
        int? usageLimit,
        DateTime? expiresAt)
    {
        DiscountCodeId = discountCodeId;
        Code = code;
        Type = type;
        Value = value;
        UsageLimit = usageLimit;
        ExpiresAt = expiresAt;
    }
}