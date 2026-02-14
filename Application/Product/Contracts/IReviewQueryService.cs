namespace Application.Product.Contracts;

/// <summary>
/// Read-side query service for reviews.
/// Returns DTOs directly — no domain entities.
/// </summary>
public interface IReviewQueryService
{
    /// <summary>
    /// Public: approved reviews for a product (paginated)
    /// </summary>
    Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        int productId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Admin: reviews by status (paginated)
    /// </summary>
    Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// User: own reviews (paginated)
    /// </summary>
    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Get review summary (average rating, count) for a product
    /// </summary>
    Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        int productId, CancellationToken ct = default);
}