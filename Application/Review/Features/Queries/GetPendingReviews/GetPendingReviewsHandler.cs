namespace Application.Review.Features.Queries.GetPendingReviews;

public class GetPendingReviewsHandler
    : IRequestHandler<GetPendingReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService;

    public GetPendingReviewsHandler(IReviewQueryService reviewQueryService)
    {
        _reviewQueryService = reviewQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetPendingReviewsQuery request, CancellationToken ct)
    {
        var result = await _reviewQueryService.GetReviewsByStatusAsync(
            request.Status, request.Page, request.PageSize, ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}