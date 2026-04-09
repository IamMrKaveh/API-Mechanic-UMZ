using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetReviewsByStatus;

public class GetReviewsByStatusHandler(IReviewQueryService reviewQueryService) : IRequestHandler<GetReviewsByStatusQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService = reviewQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(GetReviewsByStatusQuery request, CancellationToken cancellationToken)
    {
        var result = await _reviewQueryService.GetReviewsByStatusAsync(request.Status, request.Page, request.PageSize, cancellationToken);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}