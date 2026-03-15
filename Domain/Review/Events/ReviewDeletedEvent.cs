using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewDeletedEvent(ProductReviewId reviewId, int productId, int? deletedBy) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public int ProductId { get; } = productId;
    public int? DeletedBy { get; } = deletedBy;
}