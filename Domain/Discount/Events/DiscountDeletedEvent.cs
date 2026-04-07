using Domain.Discount.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Discount.Events;

public sealed class DiscountDeletedEvent(DiscountCodeId discountId, UserId? deletedBy) : DomainEvent
{
    public DiscountCodeId DiscountId { get; } = discountId;
    public UserId? DeletedBy { get; } = deletedBy;
}