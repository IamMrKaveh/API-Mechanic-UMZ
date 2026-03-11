namespace Domain.Discount.Events;

public sealed class DiscountUsageRecordedEvent : DomainEvent
{
    public DiscountUsageId UsageId { get; }
    public DiscountCodeId DiscountCodeId { get; }
    public UserId UserId { get; }
    public string OrderId { get; }
    public decimal DiscountedAmount { get; }

    public DiscountUsageRecordedEvent(
        DiscountUsageId usageId,
        DiscountCodeId discountCodeId,
        UserId userId,
        string orderId,
        decimal discountedAmount)
    {
        UsageId = usageId;
        DiscountCodeId = discountCodeId;
        UserId = userId;
        OrderId = orderId;
        DiscountedAmount = discountedAmount;
    }
}