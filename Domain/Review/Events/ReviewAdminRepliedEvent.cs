using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewAdminRepliedEvent(
    ReviewId reviewId,
    ProductId productId,
    string reply)
    : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public string Reply { get; } = reply;
}
