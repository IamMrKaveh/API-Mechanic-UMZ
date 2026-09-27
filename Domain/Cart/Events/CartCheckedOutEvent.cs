using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed record CartCheckedOutEvent(CartId CartId, UserId? UserId, int ItemCount, decimal TotalAmount) : DomainEvent;
