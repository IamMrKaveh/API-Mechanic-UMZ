using Application.Review.Contracts;
using Application.Review.Features.Shared;

namespace Application.Review.Features.Queries.GetProductReviews;

public class GetProductReviewsHandler
    : IRequestHandler<GetProductReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    private readonly IReviewQueryService _reviewQueryService;

    public GetProductReviewsHandler(IReviewQueryService reviewQueryService)
    {
        _reviewQueryService = reviewQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetProductReviewsQuery request, CancellationToken ct)
    {
        var result = await _reviewQueryService.GetApprovedProductReviewsAsync(
            request.ProductId, request.Page, request.PageSize, ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}