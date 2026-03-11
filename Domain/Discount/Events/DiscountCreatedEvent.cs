namespace Domain.Discount.Events;

public sealed class DiscountCreatedEvent(int discountId, string code) : DomainEvent
{
    public int DiscountId { get; } = discountId;
    public string Code { get; } = code;
}