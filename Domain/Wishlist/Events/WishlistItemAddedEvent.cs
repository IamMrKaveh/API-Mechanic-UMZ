using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;
using Domain.Common.Events;

namespace Domain.Wishlist.Events;

public sealed class WishlistItemAddedEvent(
    WishlistId wishlistId,
    UserId userId,
    ProductId productId) : DomainEvent
{
    public WishlistId WishlistId { get; } = wishlistId;
    public UserId UserId { get; } = userId;
    public ProductId ProductId { get; } = productId;
}