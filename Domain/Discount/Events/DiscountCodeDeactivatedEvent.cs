using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCodeDeactivatedEvent : DomainEvent
{
    public DiscountCodeId DiscountCodeId { get; }
    public string Code { get; }

    public DiscountCodeDeactivatedEvent(DiscountCodeId discountCodeId, string code)
    {
        DiscountCodeId = discountCodeId;
        Code = code;
    }
}