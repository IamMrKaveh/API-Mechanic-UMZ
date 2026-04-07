using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewDeletedEvent(ProductReviewId reviewId, ProductId productId, UserId? deletedBy) : DomainEvent
{
    public ProductReviewId ReviewId { get; } = reviewId;
    public ProductId ProductId { get; } = productId;
    public UserId? DeletedBy { get; } = deletedBy;
}