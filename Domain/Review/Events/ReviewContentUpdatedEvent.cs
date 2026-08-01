using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewContentUpdatedEvent(
    ReviewId reviewId,
    ProductId productId,
    int newRating)
    : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public int NewRating { get; } = newRating;
}
