using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Domain.Wishlist.Events;

public sealed record WishlistItemAddedEvent(
    WishlistId WishlistId,
    UserId UserId,
    ProductId ProductId) : DomainEvent;
