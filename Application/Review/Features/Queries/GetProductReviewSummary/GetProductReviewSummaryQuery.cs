namespace Application.Review.Features.Queries.GetProductReviewSummary;

public record GetProductReviewSummaryQuery(int ProductId) : IRequest<ServiceResult<ReviewSummaryDto>>;