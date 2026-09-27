using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed record CartCreatedEvent(CartId CartId, UserId? UserId, GuestToken? GuestToken) : DomainEvent;
