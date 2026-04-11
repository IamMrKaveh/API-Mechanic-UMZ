using Application.Review.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler(IUserQueryService userQueryService)
        : IRequestHandler<GetUserReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IUserQueryService _userQueryService = userQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await _userQueryService.GetUserReviewsPagedAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}