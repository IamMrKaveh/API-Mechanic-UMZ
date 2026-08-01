using Application.Review.Configuration;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;

namespace Application.Review.Features.Queries.CanReviewProduct;

public sealed class CanReviewProductHandler(
    IReviewRepository reviewRepository,
    IPurchaseVerificationService purchaseVerificationService,
    ICurrentUserService currentUser,
    IOptions<ReviewSettings> reviewSettings)
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
        OrderId? orderId = request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null;

        var hasReviewed = await reviewRepository.UserHasReviewedProductAsync(userId, productId, orderId, ct);
        var hasPurchased = await purchaseVerificationService.UserHasPurchasedProductAsync(userId, productId, ct);

        var canReview = true;
        string? reason = null;

        if (hasReviewed)
        {
            canReview = false;
            reason = "شما قبلاً برای این محصول نظر ثبت کرده‌اید.";
        }
        else if (reviewSettings.Value.RequirePurchaseVerification && !hasPurchased)
        {
            canReview = false;
            reason = "برای ثبت نظر باید ابتدا این محصول را خریداری کنید.";
        }

        var dto = new CanReviewDto(canReview, hasReviewed, hasPurchased, reason);
        return ServiceResult<CanReviewDto>.Success(dto);
    }
}
