using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCodeActivatedEvent(DiscountCodeId discountCodeId, string code) : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; } = discountCodeId;
    public string Code { get; } = code;
}