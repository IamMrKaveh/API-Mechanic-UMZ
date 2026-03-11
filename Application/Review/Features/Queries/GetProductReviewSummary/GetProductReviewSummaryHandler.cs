namespace Application.Review.Features.Queries.GetProductReviewSummary;

public sealed class GetProductReviewSummaryHandler(IReviewQueryService reviewQueryService)
        : IRequestHandler<GetProductReviewSummaryQuery, ServiceResult<ProductReviewSummaryDto>>
{
    private readonly IReviewQueryService _reviewQueryService = reviewQueryService;

    public async Task<ServiceResult<ProductReviewSummaryDto>> Handle(
        GetProductReviewSummaryQuery request,
        CancellationToken ct)
    {
        var summary = await _reviewQueryService.GetSummaryAsync(request.ProductId, ct);

        return summary is null
            ? ServiceResult<ProductReviewSummaryDto>.Failure("Review summary not found.")
            : ServiceResult<ProductReviewSummaryDto>.Success(summary);
    }
}