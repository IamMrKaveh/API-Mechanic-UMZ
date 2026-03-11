namespace Domain.Review.Interfaces;

public interface IReviewRepository
{
    Task AddAsync(ProductReview review, CancellationToken ct = default);

    void Update(ProductReview review);

    Task<bool> UserHasReviewedProductAsync(int userId, int productId, int? orderId, CancellationToken ct);

    Task<ProductReview?> GetByIdAsync(ProductReviewId id, CancellationToken ct = default);
}