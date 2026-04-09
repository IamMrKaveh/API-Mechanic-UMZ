using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public record GetReviewsByStatusQuery(
    string Status,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;