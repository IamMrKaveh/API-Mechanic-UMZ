using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Queries.CanReviewProduct;

public sealed class CanReviewProductHandler(
    IReviewRepository reviewRepository,
    IPurchaseVerificationService purchaseVerificationService,
    ICurrentUserService currentUser)
    : IQueryHandler<CanReviewProductQuery, CanReviewDto>
{
    public async Task<ServiceResult<CanReviewDto>> Handle(
        CanReviewProductQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            var anonymous = new CanReviewDto(
                CanReview: false,
                HasReviewed: false,
                HasPurchased: false,
                Reason: "برای ثبت نظر ابتدا وارد شوید.");

            return ServiceResult<CanReviewDto>.Success(anonymous);
        }

        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(currentUser.UserId!.Value);

        var hasReviewed = await reviewRepository.UserHasReviewedProductAsync(userId, productId, null, ct);
        var hasPurchased = await purchaseVerificationService.UserHasPurchasedProductAsync(userId, productId, ct);

        string? reason = null;
        var canReview = true;

        if (hasReviewed)
        {
            canReview = false;
            reason = "شما قبلاً برای این محصول نظر ثبت کرده‌اید.";
        }

        var dto = new CanReviewDto(canReview, hasReviewed, hasPurchased, reason);
        return ServiceResult<CanReviewDto>.Success(dto);
    }
}