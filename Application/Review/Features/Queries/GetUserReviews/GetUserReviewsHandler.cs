using Application.Review.Contracts;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler
    : IRequestHandler<GetUserReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService;

    public GetUserReviewsHandler(IReviewQueryService reviewQueryService)
    {
        _reviewQueryService = reviewQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request, CancellationToken ct)
    {
        var result = await _reviewQueryService.GetUserReviewsAsync(
            request.UserId, request.Page, request.PageSize, ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}