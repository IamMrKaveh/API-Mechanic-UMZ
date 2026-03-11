namespace Domain.Discount.Events;

public sealed class DiscountAppliedEvent(int discountId, int userId, int orderId) : DomainEvent
{
    public int DiscountId { get; } = discountId;
    public int UserId { get; } = userId;
    public int OrderId { get; } = orderId;
}