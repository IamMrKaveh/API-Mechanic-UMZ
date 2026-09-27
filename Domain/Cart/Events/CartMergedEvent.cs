using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed record CartMergedEvent(CartId TargetCartId, CartId SourceCartId, UserId UserId, int MergedItemCount) : DomainEvent;
