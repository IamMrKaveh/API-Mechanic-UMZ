using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Interfaces;

public interface IReviewRepository
{
    Task AddAsync(
        ProductReview review,
        CancellationToken ct = default);

    void Update(ProductReview review);

    Task<bool> UserHasReviewedProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct);

    Task<ProductReview?> GetByIdAsync(
        ReviewId id,
        CancellationToken ct = default);
}