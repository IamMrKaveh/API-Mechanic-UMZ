using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetProductReviewSummary;

public sealed record GetProductReviewSummaryQuery(
    Guid ProductId)
    : IQuery<ReviewSummaryDto>;