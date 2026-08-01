using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Services;

public sealed class ReviewDomainService
{
    private readonly IPurchaseVerificationService _purchaseVerificationService;
    private readonly IReviewRepository _reviewRepository;

    public ReviewDomainService(
        IPurchaseVerificationService purchaseVerificationService,
        IReviewRepository reviewRepository)
    {
        Guard.Against.Null(purchaseVerificationService, nameof(purchaseVerificationService));
        Guard.Against.Null(reviewRepository, nameof(reviewRepository));

        _purchaseVerificationService = purchaseVerificationService;
        _reviewRepository = reviewRepository;
    }

    public async Task<ServiceResult<ProductReview>> SubmitReviewAsync(
        ProductId productId,
        UserId userId,
        Rating rating,
        string? title,
        string? comment,
        OrderId? orderId,
        bool requirePurchaseVerification,
        CancellationToken ct = default)
    {
        Guard.Against.Null(productId, nameof(productId));
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(rating, nameof(rating));

        bool alreadyReviewed = await _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, orderId, ct);

        if (alreadyReviewed)
            return ServiceResult<ProductReview>.Failure(
                Error.Validation("Review.AlreadyExists", "کاربر قبلاً برای این محصول نظر ثبت کرده است."));

        bool isVerifiedPurchase = false;

        if (requirePurchaseVerification)
        {
            isVerifiedPurchase = await _purchaseVerificationService
                .UserHasPurchasedProductAsync(userId, productId, ct);

            if (!isVerifiedPurchase)
                return ServiceResult<ProductReview>.Failure(
                    Error.Validation("Review.NotPurchased", "برای ثبت نظر باید محصول را خریداری کرده باشید."));
        }
        else
        {
            isVerifiedPurchase = await _purchaseVerificationService
                .UserHasPurchasedProductAsync(userId, productId, ct);
        }

        var review = ProductReview.Create(
            productId,
            userId,
            rating,
            title,
            comment,
            isVerifiedPurchase,
            orderId);

        return ServiceResult<ProductReview>.Success(review);
    }
}
