using System.Data.Common;
using Application.Review.Features.Shared;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandler(
    IReviewQueryService reviewQueryService,
    ICurrentUserService currentUserService,
    ILogger<GetUserReviewsHandler> logger)
    : IQueryHandler<GetUserReviewsQuery, PaginatedResult<ProductReviewDto>>
{
    public async Task<ServiceResult<PaginatedResult<ProductReviewDto>>> Handle(
        GetUserReviewsQuery request, CancellationToken ct)
    {
        if (currentUserService.UserId is null || currentUserService.UserId.Value == Guid.Empty)
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Unauthorized("برای مشاهده نظرات باید وارد شوید.");

        try
        {
            var userId = UserId.From(currentUserService.UserId.Value);

            var result = await reviewQueryService.GetUserReviewsAsync(
                userId,
                request.Page,
                request.PageSize,
                ct);

            return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading reviews for current user {UserId}", currentUserService.UserId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Unexpected("خطا در بارگذاری نظرات کاربر.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading reviews for current user {UserId}", currentUserService.UserId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت نظرات."));
        }
    }
}
