using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCodeActivatedEvent : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; }
    public string Code { get; }

    public DiscountCodeActivatedEvent(DiscountCodeId discountCodeId, string code)
    {
        DiscountCodeId = discountCodeId;
        Code = code;
    }
}