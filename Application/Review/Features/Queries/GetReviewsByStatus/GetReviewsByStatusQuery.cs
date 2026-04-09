using Application.Common.Results;
using Application.Review.Features.Shared;
using SharedKernel.Models;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public record GetReviewsByStatusQuery(string Status, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;