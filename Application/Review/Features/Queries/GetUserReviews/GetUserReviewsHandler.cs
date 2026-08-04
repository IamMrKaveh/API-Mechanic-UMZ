using Application.Common.Authorization;
using Application.Review.Features.Shared;
using Domain.User.ValueObjects;

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
        var authCheck = AuthorizationGuard.EnsureAuthenticated<PaginatedResult<ProductReviewDto>>(currentUserService);
        if (authCheck.IsFailure)
            return authCheck;

        var targetUserId = request.UserId ?? currentUserService.UserId!.Value;

        var ownershipCheck = AuthorizationGuard.EnsureOwnerOrAdmin<PaginatedResult<ProductReviewDto>>(
            currentUserService, targetUserId);
        if (ownershipCheck.IsFailure)
        {
            logger.LogWarning(
                "IDOR attempt: user {ActorUserId} tried to access reviews of user {TargetUserId}",
                currentUserService.UserId, targetUserId);
            return ownershipCheck;
        }

        try
        {
            var userId = UserId.From(targetUserId);

            var result = await reviewQueryService.GetUserReviewsAsync(
                userId,
                request.Page,
                request.PageSize,
                ct);

            return ServiceResult<PaginatedResult<ProductReviewDto>>.Success(result);
        }
        catch (NullReferenceException ex)
        {
            logger.LogError(ex, "Null projection error while loading reviews for user {UserId}", targetUserId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Unexpected("خطا در بارگذاری نظرات کاربر.");
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while loading reviews for user {UserId}", targetUserId);
            return ServiceResult<PaginatedResult<ProductReviewDto>>.Failure(
                Error.Infrastructure("خطا در ارتباط با پایگاه داده هنگام دریافت نظرات."));
        }
    }
}
