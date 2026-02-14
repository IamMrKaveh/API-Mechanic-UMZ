namespace Application.User.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler
    : IRequestHandler<GetUserReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserReviewsHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _userQueryService.GetUserReviewsPagedAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}