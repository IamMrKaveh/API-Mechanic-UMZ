using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetProductReviews;

public record GetProductReviewsQuery(
    Guid ProductId,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;