namespace Application.Review.Contracts;

public interface IReviewQueryService
{
    Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        int productId, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default);

    Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        int productId, CancellationToken ct = default);
}