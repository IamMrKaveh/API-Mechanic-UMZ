namespace Domain.Discount.Events;

public sealed class DiscountUsageConfirmedEvent(int usageId, int orderId, int discountId) : DomainEvent
{
    public int UsageId { get; } = usageId;
    public int OrderId { get; } = orderId;
    public int DiscountId { get; } = discountId;
}