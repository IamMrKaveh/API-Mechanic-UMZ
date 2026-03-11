namespace Domain.Discount.Events;

public sealed class DiscountActivatedEvent(int discountId) : DomainEvent
{
    public int DiscountId { get; } = discountId;
}