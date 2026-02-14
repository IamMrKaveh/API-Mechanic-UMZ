namespace Domain.Discount.Events;

public sealed class DiscountCreatedEvent : DomainEvent
{
    public int DiscountId { get; }
    public string Code { get; }

    public DiscountCreatedEvent(int discountId, string code)
    {
        DiscountId = discountId;
        Code = code;
    }
}