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

    void Remove(ProductReview review);

    Task<bool> UserHasReviewedProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct);

    Task<ProductReview?> GetByIdAsync(
        ReviewId id,
        CancellationToken ct = default);

    Task<ProductReview?> GetByIdIncludingDeletedAsync(
        ReviewId id,
        CancellationToken ct = default);

    Task<ProductReview?> GetByUserAndProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductReview>> ListByUserAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductReview>> ListByProductAsync(
        ProductId productId,
        CancellationToken ct = default);
}
