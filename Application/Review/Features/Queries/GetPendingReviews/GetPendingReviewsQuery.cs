using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetPendingReviews;

public record GetPendingReviewsQuery(string Status = "Pending") : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;