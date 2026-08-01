using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewRejectedEvent(
    ReviewId reviewId,
    ProductId productId,
    string? reason)
    : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public string? Reason { get; } = reason;
}
