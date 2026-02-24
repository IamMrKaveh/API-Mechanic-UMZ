namespace Domain.Review.Events;

public sealed class ReviewStatusChangedEvent : DomainEvent
{
    public int ReviewId { get; }
    public int ProductId { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public bool IsApproved { get; }

    public ReviewStatusChangedEvent(int reviewId, int productId, string oldStatus, string newStatus)
    {
        ReviewId = reviewId;
        ProductId = productId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        IsApproved = newStatus == "Approved";
    }
}