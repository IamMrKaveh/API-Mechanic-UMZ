using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Queries.GetProductReviews;

public class GetProductReviewsHandler(
    IReviewQueryService reviewQueryService,
    ICurrentUserService currentUserService,
    ILogger<GetProductReviewsHandler> logger)
    : IQueryHandler<GetProductReviewsQuery, PaginatedResult<ProductReviewDto>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetProductReviewsQuery request, CancellationToken ct)
    {
        try
        {
            var productId = ProductId.From(request.ProductId);

            UserId? currentUserId = null;
            if (currentUserService.IsAuthenticated
                && currentUserService.UserId is { } uid
                && uid != Guid.Empty)
            {
                currentUserId = UserId.From(uid);
            }

            var result = await reviewQueryService.GetApprovedProductReviewsAsync(
                productId,
                request.Page,
                request.PageSize,
                request.SortBy,
                request.MinRating,
                request.VerifiedOnly,
                currentUserId,
                ct);

            return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading approved reviews for product {ProductId}", request.ProductId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Unexpected("خطا در بارگذاری نظرات محصول.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading approved reviews for product {ProductId}", request.ProductId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت نظرات."));
        }
    }
}
