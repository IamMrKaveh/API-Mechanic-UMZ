using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Events;

public sealed record DiscountCodeAppliedEvent(
    DiscountCodeId DiscountCodeId,
    string Code,
    UserId UserId,
    OrderId OrderId,
    decimal DiscountedAmount,
    int UsageCount) : DomainEvent;
