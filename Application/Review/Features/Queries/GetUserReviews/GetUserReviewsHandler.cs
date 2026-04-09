using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler(IReviewQueryService reviewQueryService) : IRequestHandler<GetUserReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService = reviewQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request, CancellationToken ct)
    {
        var result = await _reviewQueryService.GetUserReviewsAsync(
            request.UserId, request.Page, request.PageSize, ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}