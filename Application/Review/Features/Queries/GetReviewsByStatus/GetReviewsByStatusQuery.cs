namespace Application.Review.Features.Queries.GetReviewsByStatus;

public record GetReviewsByStatusQuery(string Status, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;