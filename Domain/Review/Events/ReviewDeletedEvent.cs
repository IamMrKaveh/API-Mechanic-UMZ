using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewDeletedEvent(ReviewId reviewId, ProductId productId, UserId? deletedBy) : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public UserId? DeletedBy { get; } = deletedBy;
}