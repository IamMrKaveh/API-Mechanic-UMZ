using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;

namespace Domain.Review.Interfaces;

public interface IReviewRepository
{
    Task AddAsync(ProductReview review, CancellationToken ct = default);

    void Update(ProductReview review);

    Task<bool> UserHasReviewedProductAsync(int userId, int productId, int? orderId, CancellationToken ct);

    Task<ProductReview?> GetByIdAsync(ReviewId id, CancellationToken ct = default);
}