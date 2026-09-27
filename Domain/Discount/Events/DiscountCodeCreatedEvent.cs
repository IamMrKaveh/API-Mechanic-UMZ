using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;

namespace Domain.Discount.Events;

public sealed record DiscountCodeCreatedEvent(
    DiscountCodeId DiscountCodeId,
    string Code,
    DiscountType Type,
    decimal Value,
    int? UsageLimit,
    DateTime? ExpiresAt) : DomainEvent;
