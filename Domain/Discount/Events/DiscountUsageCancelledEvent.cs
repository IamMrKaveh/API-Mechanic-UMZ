namespace Domain.Discount.Events;

public sealed class DiscountUsageCancelledEvent : DomainEvent
{
    public int DiscountId { get; }
    public int OrderId { get; }

    public DiscountUsageCancelledEvent(int discountId, int orderId)
    {
        DiscountId = discountId;
        OrderId = orderId;
    }
}