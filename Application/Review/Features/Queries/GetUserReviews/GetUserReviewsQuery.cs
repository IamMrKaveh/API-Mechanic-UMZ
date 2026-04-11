using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetUserReviews;

public record GetUserReviewsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;