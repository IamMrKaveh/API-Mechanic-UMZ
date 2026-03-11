namespace Application.Review.Features.Queries.GetProductReviewSummary;

public sealed record GetProductReviewSummaryQuery(int ProductId)
    : IRequest<ServiceResult<ProductReviewSummaryDto>>, ICacheableQuery
{
    public string CacheKey => CacheKeys.ReviewSummary(ProductId);
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
}