using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Review.Features.Queries.GetProductReviewSummary;

public sealed class GetProductReviewSummaryHandler(IReviewQueryService reviewQueryService)
        : IRequestHandler<GetProductReviewSummaryQuery, ServiceResult<ReviewSummaryDto>>
{
    public async Task<ServiceResult<ReviewSummaryDto>> Handle(
        GetProductReviewSummaryQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var summary = await reviewQueryService.GetProductReviewSummaryAsync(productId, ct);

        return summary is null
            ? ServiceResult<ReviewSummaryDto>.Failure("Review summary not found.")
            : ServiceResult<ReviewSummaryDto>.Success(summary);
    }
}