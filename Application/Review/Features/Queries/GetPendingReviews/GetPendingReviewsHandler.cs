using Application.Review.Contracts;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetPendingReviews;

public sealed class GetPendingReviewsHandler(
    IReviewQueryService reviewQueryService) : IRequestHandler<GetPendingReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetPendingReviewsQuery request, CancellationToken ct)
    {
        var result = await reviewQueryService.GetReviewsByStatusAsync(
            request.Status,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}