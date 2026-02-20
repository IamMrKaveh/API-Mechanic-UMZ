using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetUserReviews;

public record GetUserReviewsQuery(int UserId, int Page = 1, int PageSize = 10)
    : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;