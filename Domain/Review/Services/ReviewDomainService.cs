using Domain.Review.Aggregates;
using Domain.Review.Interfaces;

namespace Domain.Review.Services;

public sealed class ReviewDomainService
{
    private readonly IPurchaseVerificationService _purchaseVerificationService;

    public ReviewDomainService(IPurchaseVerificationService purchaseVerificationService)
    {
        Guard.Against.Null(purchaseVerificationService, nameof(purchaseVerificationService));
        _purchaseVerificationService = purchaseVerificationService;
    }

    public async Task<Result<ProductReview>> SubmitReviewAsync(
        int productId,
        int userId,
        int rating,
        string? title,
        string? comment,
        int? orderId,
        bool requirePurchaseVerification,
        Func<int, int, int?, CancellationToken, Task<bool>> hasExistingReviewCheck,
        CancellationToken ct = default)
    {
        Guard.Against.NegativeOrZero(productId, nameof(productId));
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        bool alreadyReviewed = await hasExistingReviewCheck(userId, productId, orderId, ct);
        if (alreadyReviewed)
            return Result<ProductReview>.Failure("کاربر قبلاً برای این محصول نظر ثبت کرده است.");

        bool isVerifiedPurchase = false;

        if (requirePurchaseVerification)
        {
            isVerifiedPurchase = await _purchaseVerificationService.UserHasPurchasedProductAsync(userId, productId, ct);

            if (!isVerifiedPurchase)
                return Result<ProductReview>.Failure("برای ثبت نظر باید محصول را خریداری کرده باشید.");
        }

        var review = ProductReview.Create(
            productId,
            userId,
            rating,
            title,
            comment,
            isVerifiedPurchase,
            orderId);

        return Result<ProductReview>.Success(review);
    }
}