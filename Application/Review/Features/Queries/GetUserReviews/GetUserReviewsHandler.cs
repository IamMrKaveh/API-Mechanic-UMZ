using Application.Review.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler(
    IReviewQueryService reviewQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUserReviewsQuery, PaginatedResult<ProductReviewDto>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);

        var result = await reviewQueryService.GetUserReviewsAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}