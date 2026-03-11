namespace Domain.Discount.Events;

public sealed class DiscountUsageCancelledEvent(int discountId, int orderId) : DomainEvent
{
    public int DiscountId { get; } = discountId;
    public int OrderId { get; } = orderId;
}