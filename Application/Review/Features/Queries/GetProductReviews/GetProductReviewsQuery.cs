using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetProductReviews;

public record GetProductReviewsQuery(Guid ProductId) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;