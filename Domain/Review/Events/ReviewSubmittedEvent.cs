using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewSubmittedEvent(ProductReviewId reviewId, ProductId productId, UserId userId, Rating rating) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public UserId UserId { get; } = userId;
    public Rating Rating { get; } = rating;
}