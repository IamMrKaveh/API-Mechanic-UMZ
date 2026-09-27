using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed record DiscountCodeActivatedEvent(DiscountCodeId DiscountCodeId, string Code) : DomainEvent;
