using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountCreatedEvent(DiscountCodeId discountId, string code) : DomainEvent
{
    public DiscountCodeId DiscountId { get; } = discountId;
    public string Code { get; } = code;
}