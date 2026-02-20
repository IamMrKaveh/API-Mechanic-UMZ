namespace Application.Review.Features.Queries.GetProductReviewSummary;

public class GetProductReviewSummaryHandler : IRequestHandler<GetProductReviewSummaryQuery, ServiceResult<ReviewSummaryDto>>
{
    private readonly IReviewQueryService _reviewQueryService;
    private readonly ICacheService _cacheService;

    public GetProductReviewSummaryHandler(IReviewQueryService reviewQueryService, ICacheService cacheService)
    {
        _reviewQueryService = reviewQueryService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<ReviewSummaryDto>> Handle(GetProductReviewSummaryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"review:summary:{request.ProductId}";

        var cached = await _cacheService.GetAsync<ReviewSummaryDto>(cacheKey);
        if (cached != null)
        {
            return ServiceResult<ReviewSummaryDto>.Success(cached);
        }

        var summary = await _reviewQueryService.GetProductReviewSummaryAsync(request.ProductId, cancellationToken);

        await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(30)); // Aggressive caching for summary

        return ServiceResult<ReviewSummaryDto>.Success(summary);
    }
}