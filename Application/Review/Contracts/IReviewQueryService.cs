using Application.Review.Features.Queries.AdminReviewStats;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Contracts;

public interface IReviewQueryService
{
    Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        ProductId productId,
        int page,
        int pageSize,
        string sortBy,
        int? minRating,
        bool verifiedOnly,
        UserId? currentUserId,
        CancellationToken cancellationToken);

    Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        AdminReviewFilter filter,
        CancellationToken cancellationToken);

    Task<ReviewSummaryDto?> GetProductReviewSummaryAsync(
        ProductId productId,
        CancellationToken cancellationToken);

    Task<AdminReviewStatsDto> GetAdminReviewStatsAsync(CancellationToken cancellationToken);

    Task<ProductReviewDto?> GetByIdAsync(
        ReviewId reviewId,
        UserId? currentUserId,
        CancellationToken cancellationToken);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
