namespace Application.Review.Features.Queries.CanReviewProduct;

public sealed record CanReviewProductQuery(
    Guid ProductId)
    : IQuery<CanReviewDto>;

public sealed record CanReviewDto(
    bool CanReview,
    bool HasReviewed,
    bool HasPurchased,
    string? Reason);