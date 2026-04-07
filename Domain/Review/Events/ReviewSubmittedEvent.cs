using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewSubmittedEvent(ReviewId reviewId, ProductId productId, UserId userId, Rating rating) : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public UserId UserId { get; } = userId;
    public Rating Rating { get; } = rating;
}