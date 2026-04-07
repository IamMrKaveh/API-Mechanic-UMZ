using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Domain.Wishlist.Events;

public sealed class WishlistItemRemovedEvent(
    WishlistId wishlistId,
    UserId userId,
    ProductId productId) : DomainEvent
{
    public WishlistId WishlistId { get; } = wishlistId;
    public UserId UserId { get; } = userId;
    public ProductId ProductId { get; } = productId;
}