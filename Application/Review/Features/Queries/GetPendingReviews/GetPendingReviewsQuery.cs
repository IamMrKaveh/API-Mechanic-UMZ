using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetPendingReviews;

public record GetPendingReviewsQuery(string Status = "Pending", int Page = 1, int PageSize = 20)
    : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;