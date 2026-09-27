using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed record DiscountCodeDeactivatedEvent(DiscountCodeId DiscountCodeId, string Code) : DomainEvent;
