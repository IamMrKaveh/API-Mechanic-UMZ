using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;

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
        ProductId productId,
        UserId userId,
        Rating rating,
        string? title,
        string? comment,
        OrderId? orderId,
        bool requirePurchaseVerification,
        Func<UserId, ProductId, OrderId?, CancellationToken, Task<bool>> hasExistingReviewCheck,
        CancellationToken ct = default)
    {
        Guard.Against.Null(productId, nameof(productId));
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(rating, nameof(rating));

        bool alreadyReviewed = await hasExistingReviewCheck(userId, productId, orderId, ct);
        if (alreadyReviewed)
            return Result<ProductReview>.Failure(Error.Validation("Review.AlreadyExists", "کاربر قبلاً برای این محصول نظر ثبت کرده است."));

        bool isVerifiedPurchase = false;

        if (requirePurchaseVerification)
        {
            int numericUserId = (int)Convert.ChangeType(userId.Value, typeof(int));
            int numericProductId = (int)Convert.ChangeType(productId.Value, typeof(int));

            isVerifiedPurchase = await _purchaseVerificationService.UserHasPurchasedProductAsync(numericUserId, numericProductId, ct);

            if (!isVerifiedPurchase)
                return Result<ProductReview>.Failure(Error.Validation("Review.NotPurchased", "برای ثبت نظر باید محصول را خریداری کرده باشید."));
        }

        var numericUserIdCreate = (int)Convert.ChangeType(userId.Value, typeof(int));
        var numericProductIdCreate = (int)Convert.ChangeType(productId.Value, typeof(int));
        int? numericOrderIdCreate = orderId != null ? (int?)Convert.ChangeType(orderId.Value, typeof(int)) : null;

        var review = ProductReview.Create(
            numericProductIdCreate,
            numericUserIdCreate,
            rating,
            title,
            comment,
            isVerifiedPurchase,
            numericOrderIdCreate);

        return Result<ProductReview>.Success(review);
    }
}