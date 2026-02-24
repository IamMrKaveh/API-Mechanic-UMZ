namespace Domain.Discount.Events;

public sealed class DiscountUsageConfirmedEvent : DomainEvent
{
    public int UsageId { get; }
    public int OrderId { get; }
    public int DiscountId { get; }

    public DiscountUsageConfirmedEvent(int usageId, int orderId, int discountId)
    {
        UsageId = usageId;
        OrderId = orderId;
        DiscountId = discountId;
    }
}