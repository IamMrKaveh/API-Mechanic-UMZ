using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewApprovedEvent(ProductReviewId reviewId, ProductId productId, Rating rating) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public Rating Rating { get; } = rating;
}