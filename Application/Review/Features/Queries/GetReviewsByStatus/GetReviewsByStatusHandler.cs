using Application.Review.Contracts;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public sealed class GetReviewsByStatusHandler(
    IReviewQueryService reviewQueryService) : IRequestHandler<GetReviewsByStatusQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetReviewsByStatusQuery request, CancellationToken cancellationToken)
    {
        var result = await reviewQueryService.GetReviewsByStatusAsync(
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}