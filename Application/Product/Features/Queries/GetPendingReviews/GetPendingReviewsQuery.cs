namespace Application.Product.Features.Queries.GetPendingReviews;

public record GetPendingReviewsQuery(string Status = "Pending", int Page = 1, int PageSize = 20)
    : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;