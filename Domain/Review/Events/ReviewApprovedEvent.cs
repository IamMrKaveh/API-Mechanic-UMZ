namespace Domain.Review.Events;

public sealed class ReviewApprovedEvent(ProductReviewId reviewId, int productId, int rating) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public int ProductId { get; } = productId;
    public int Rating { get; } = rating;
}