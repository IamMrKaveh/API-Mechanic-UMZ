using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed class CartMergedEvent(CartId targetCartId, CartId sourceCartId, UserId userId, int mergedItemCount) : DomainEvent
{
    public CartId TargetCartId { get; } = targetCartId;
    public CartId SourceCartId { get; } = sourceCartId;
    public UserId UserId { get; } = userId;
    public int MergedItemCount { get; } = mergedItemCount;
}