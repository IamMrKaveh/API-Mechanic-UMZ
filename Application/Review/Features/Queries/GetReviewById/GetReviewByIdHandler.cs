using Application.Review.Features.Shared;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Queries.GetReviewById;

public sealed class GetReviewByIdHandler(
    IReviewQueryService reviewQueryService)
    : IQueryHandler<GetReviewByIdQuery, ProductReviewDto>
{
    public async Task<ServiceResult<ProductReviewDto>> Handle(
        GetReviewByIdQuery request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var dto = await reviewQueryService.GetByIdAsync(reviewId, ct);

        return dto is null
            ? ServiceResult<ProductReviewDto>.NotFound("نظر یافت نشد.")
            : ServiceResult<ProductReviewDto>.Success(dto);
    }
}