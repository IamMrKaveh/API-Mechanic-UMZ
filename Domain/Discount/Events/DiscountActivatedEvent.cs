namespace Domain.Discount.Events;

public sealed class DiscountActivatedEvent : DomainEvent
{
    public int DiscountId { get; }

    public DiscountActivatedEvent(int discountId)
    {
        DiscountId = discountId;
    }
}