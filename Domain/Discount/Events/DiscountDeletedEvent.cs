namespace Domain.Discount.Events;

public sealed class DiscountDeletedEvent : DomainEvent
{
    public int DiscountId { get; }
    public int? DeletedBy { get; }

    public DiscountDeletedEvent(int discountId, int? deletedBy)
    {
        DiscountId = discountId;
        DeletedBy = deletedBy;
    }
}