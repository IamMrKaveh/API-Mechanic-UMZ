using System.Data.Common;
using Application.Review.Features.Shared;
using Domain.Review.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Review.Features.Queries.GetReviewById;

public sealed class GetReviewByIdHandler(
    IReviewQueryService reviewQueryService,
    ILogger<GetReviewByIdHandler> logger)
    : IQueryHandler<GetReviewByIdQuery, ProductReviewDto>
{
    public async Task<ServiceResult<ProductReviewDto>> Handle(
        GetReviewByIdQuery request, CancellationToken ct)
    {
        try
        {
            var reviewId = ReviewId.From(request.ReviewId);

            var dto = await reviewQueryService.GetByIdAsync(reviewId, ct);

            return dto is null
                ? ServiceResult<ProductReviewDto>.NotFound("نظر یافت نشد.")
                : ServiceResult<ProductReviewDto>.Success(dto);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading review {ReviewId}", request.ReviewId);
            return ServiceResult<ProductReviewDto>.Unexpected("خطا در بارگذاری نظر.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading review {ReviewId}", request.ReviewId);
            return ServiceResult<ProductReviewDto>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت نظر."));
        }
    }
}
