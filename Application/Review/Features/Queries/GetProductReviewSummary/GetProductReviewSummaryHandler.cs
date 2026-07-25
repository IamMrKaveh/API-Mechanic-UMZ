using System.Data.Common;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Review.Features.Queries.GetProductReviewSummary;

public sealed class GetProductReviewSummaryHandler(
    IReviewQueryService reviewQueryService,
    ILogger<GetProductReviewSummaryHandler> logger)
    : IQueryHandler<GetProductReviewSummaryQuery, ReviewSummaryDto>
{
    public async Task<ServiceResult<ReviewSummaryDto>> Handle(
        GetProductReviewSummaryQuery request,
        CancellationToken ct)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);

            var summary = await reviewQueryService.GetProductReviewSummaryAsync(productId, ct);

            return summary is null
                ? ServiceResult<ReviewSummaryDto>.NotFound("خلاصه نظرات یافت نشد.")
                : ServiceResult<ReviewSummaryDto>.Success(summary);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading review summary for product {ProductId}", request.ProductId);
            return ServiceResult<ReviewSummaryDto>.Unexpected("خطا در بارگذاری خلاصه نظرات.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading review summary for product {ProductId}", request.ProductId);
            return ServiceResult<ReviewSummaryDto>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت خلاصه نظرات."));
        }
    }
}
