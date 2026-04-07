using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewApprovedEvent(ReviewId reviewId, ProductId productId, Rating rating) : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public Rating Rating { get; } = rating;
}