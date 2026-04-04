using Application.Common.Results;
using Application.Review.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;

namespace Application.Review.Features.Queries.GetUserReviews;

public record GetUserReviewsQuery(
    UserId UserId,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<ProductReviewDto>>>;