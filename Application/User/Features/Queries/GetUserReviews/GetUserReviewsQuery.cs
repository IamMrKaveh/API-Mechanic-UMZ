using Application.Review.Features.Shared;

namespace Application.User.Features.Queries.GetUserReviews;

public record GetUserReviewsQuery(Guid UserId) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;