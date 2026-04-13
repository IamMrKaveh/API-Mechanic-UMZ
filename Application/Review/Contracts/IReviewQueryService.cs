using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Contracts;

public interface IReviewQueryService
{
    Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        ProductId productId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetPendingReviewsAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status,
        int page,
        int pageSize,
        CancellationToken ct = default);
}