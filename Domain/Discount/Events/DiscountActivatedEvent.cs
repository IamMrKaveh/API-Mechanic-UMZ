using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountActivatedEvent(DiscountCodeId discountId) : DomainEvent
{
    public DiscountCodeId DiscountId { get; } = discountId;
}