namespace Domain.Review.Events;

public sealed class ReviewSubmittedEvent(ProductReviewId reviewId, int productId, int userId, int rating) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public int ProductId { get; } = productId;
    public int UserId { get; } = userId;
    public int Rating { get; } = rating;
}