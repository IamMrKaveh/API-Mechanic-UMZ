namespace Domain.Cart.Events;

public class CartMergedEvent : DomainEvent
{
    public int TargetCartId { get; }
    public int SourceCartId { get; }

    public CartMergedEvent(int targetCartId, int sourceCartId)
    {
        TargetCartId = targetCartId;
        SourceCartId = sourceCartId;
    }
}