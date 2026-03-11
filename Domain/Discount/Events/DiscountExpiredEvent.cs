namespace Domain.Discount.Events;

public sealed class DiscountExpiredEvent(int discountId) : DomainEvent
{
    public int DiscountId { get; } = discountId;
}