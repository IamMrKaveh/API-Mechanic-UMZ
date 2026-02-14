namespace Application.Product.Features.Queries.GetReviewsByStatus;

public record GetReviewsByStatusQuery(string Status, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;