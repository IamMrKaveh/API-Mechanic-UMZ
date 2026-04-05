using Domain.Common.Guards;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            isVerifiedPurchase = await _purchaseVerificationService.UserHasPurchasedProductAsync(userId, productId, ct);

            if (!isVerifiedPurchase)
                return Result<ProductReview>.Failure(Error.Validation("Review.NotPurchased", "برای ثبت نظر باید محصول را خریداری کرده باشید."));
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