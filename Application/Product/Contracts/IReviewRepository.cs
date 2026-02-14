namespace Application.Product.Contracts;

public interface IReviewRepository
{
    Task<ProductReview?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IEnumerable<ProductReview> Reviews, int TotalCount)> GetByProductIdAsync(
        int productId, string? status, int page, int pageSize, CancellationToken ct = default);

    Task<(IEnumerable<ProductReview> Reviews, int TotalCount)> GetPendingReviewsAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task AddAsync(ProductReview review, CancellationToken ct = default);

    void Update(ProductReview review);

    Task<bool> UserHasReviewedProductAsync(int userId, int productId, int? orderId, CancellationToken ct);

    Task<bool> UserHasPurchasedProductAsync(int userId, int productId, CancellationToken ct);
}