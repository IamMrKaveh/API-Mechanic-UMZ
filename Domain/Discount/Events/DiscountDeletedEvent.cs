namespace Domain.Discount.Events;

public sealed class DiscountDeletedEvent(int discountId, int? deletedBy) : DomainEvent
{
    public int DiscountId { get; } = discountId;
    public int? DeletedBy { get; } = deletedBy;
}