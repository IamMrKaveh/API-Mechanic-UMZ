namespace Domain.Wishlist.Events;

public sealed record WishlistItemAddedEvent(
    WishlistId WishlistId,
    UserId UserId,
    ProductId ProductId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}