namespace Domain.Discount.Events;

public sealed class DiscountDeactivatedEvent : DomainEvent
{
    public int DiscountId { get; }

    public DiscountDeactivatedEvent(int discountId)
    {
        DiscountId = discountId;
    }
}