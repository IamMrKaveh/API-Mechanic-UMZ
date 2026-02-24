namespace Domain.Discount.Events;

public sealed class DiscountExpiredEvent : DomainEvent
{
    public int DiscountId { get; }

    public DiscountExpiredEvent(int discountId)
    {
        DiscountId = discountId;
    }
}