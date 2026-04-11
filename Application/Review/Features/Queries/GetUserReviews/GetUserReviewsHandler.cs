using Application.Review.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler(IReviewQueryService reviewQueryService) : IRequestHandler<GetUserReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await reviewQueryService.GetUserReviewsAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}