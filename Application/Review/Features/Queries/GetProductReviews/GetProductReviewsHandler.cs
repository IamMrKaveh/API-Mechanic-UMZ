using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Review.Features.Queries.GetProductReviews;

public class GetProductReviewsHandler(IReviewQueryService reviewQueryService)
        : IRequestHandler<GetProductReviewsQuery, ServiceResult<PaginatedResult<ProductReviewDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetProductReviewsQuery request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await reviewQueryService.GetApprovedProductReviewsAsync(
            productId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
    }
}