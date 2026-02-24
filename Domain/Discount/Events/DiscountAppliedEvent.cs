namespace Domain.Discount.Events;

public sealed class DiscountAppliedEvent : DomainEvent
{
    public int DiscountId { get; }
    public int UserId { get; }
    public int OrderId { get; }

    public DiscountAppliedEvent(int discountId, int userId, int orderId)
    {
        DiscountId = discountId;
        UserId = userId;
        OrderId = orderId;
    }
}