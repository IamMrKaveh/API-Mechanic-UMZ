using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public record GetReviewsByStatusQuery(string Status) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;