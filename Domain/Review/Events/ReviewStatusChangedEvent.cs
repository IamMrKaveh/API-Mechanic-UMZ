using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewStatusChangedEvent(ProductReviewId reviewId, int productId, string oldStatus, string newStatus) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public int ProductId { get; } = productId;
    public string OldStatus { get; } = oldStatus;
    public string NewStatus { get; } = newStatus;
    public bool IsApproved { get; } = newStatus == "Approved";
}