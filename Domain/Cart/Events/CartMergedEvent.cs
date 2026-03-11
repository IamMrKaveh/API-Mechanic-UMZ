namespace Domain.Cart.Events;

public sealed class CartMergedEvent : DomainEvent
{
    public Guid TargetCartId { get; }
    public Guid SourceCartId { get; }
    public Guid UserId { get; }
    public int MergedItemCount { get; }

    public CartMergedEvent(Guid targetCartId, Guid sourceCartId, Guid userId, int mergedItemCount)
    {
        TargetCartId = targetCartId;
        SourceCartId = sourceCartId;
        UserId = userId;
        MergedItemCount = mergedItemCount;
    }
}