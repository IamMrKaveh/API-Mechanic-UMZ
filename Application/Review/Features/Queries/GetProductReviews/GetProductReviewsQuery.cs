namespace Application.Review.Features.Queries.GetProductReviews;

public record GetProductReviewsQuery(int ProductId, int Page = 1, int PageSize = 10)
    : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;