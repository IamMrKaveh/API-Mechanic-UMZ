using Application.Review.Features.Shared;

namespace Application.Review.Contracts;

public interface IReviewQueryService
{
    Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        Guid productId, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetPendingReviewsAsync(
        int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<ReviewSummaryDto> GetProductReviewSummaryAsync(Guid productId, CancellationToken ct = default);
}