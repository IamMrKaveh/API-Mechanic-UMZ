using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewStatusChangedEvent(ReviewId reviewId, ProductId productId, string oldStatus, string newStatus) : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public string OldStatus { get; } = oldStatus;
    public string NewStatus { get; } = newStatus;
    public bool IsApproved { get; } = newStatus == "Approved";
}