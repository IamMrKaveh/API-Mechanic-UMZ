namespace Domain.Discount.Events;

public sealed class DiscountCodeAppliedEvent : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; }
    public string Code { get; }
    public UserId UserId { get; }
    public string OrderId { get; }
    public decimal DiscountedAmount { get; }
    public int UsageCount { get; }

    public DiscountCodeAppliedEvent(
        DiscountCodeId discountCodeId,
        string code,
        UserId userId,
        string orderId,
        decimal discountedAmount,
        int usageCount)
    {
        DiscountCodeId = discountCodeId;
        Code = code;
        UserId = userId;
        OrderId = orderId;
        DiscountedAmount = discountedAmount;
        UsageCount = usageCount;
    }
}