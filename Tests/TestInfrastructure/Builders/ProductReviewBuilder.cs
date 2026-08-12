using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductReviewBuilder
{
    private ProductId _productId = ProductId.NewId();
    private UserId _userId = UserId.NewId();
    private Rating _rating = new RatingBuilder().WithValue(4).Build();
    private string? _title = "عنوان پیش‌فرض";
    private string? _comment = "متن نظر پیش‌فرض";
    private bool _isVerifiedPurchase = true;
    private OrderId? _orderId = OrderId.NewId();

    public ProductReviewBuilder WithProductId(ProductId productId)
    {
        _productId = productId;
        return this;
    }

    public ProductReviewBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public ProductReviewBuilder WithRating(Rating rating)
    {
        _rating = rating;
        return this;
    }

    public ProductReviewBuilder WithRating(int value)
    {
        _rating = Rating.Create(value);
        return this;
    }

    public ProductReviewBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    public ProductReviewBuilder WithComment(string? comment)
    {
        _comment = comment;
        return this;
    }

    public ProductReviewBuilder WithVerifiedPurchase(bool isVerifiedPurchase)
    {
        _isVerifiedPurchase = isVerifiedPurchase;
        return this;
    }

    public ProductReviewBuilder WithOrderId(OrderId? orderId)
    {
        _orderId = orderId;
        return this;
    }

    public ProductReviewBuilder WithoutOrderId()
    {
        _orderId = null;
        return this;
    }

    public ProductReview Build() =>
        ProductReview.Create(_productId, _userId, _rating, _title, _comment, _isVerifiedPurchase, _orderId);

    public ProductReview BuildApproved()
    {
        var review = Build();
        review.Approve();
        return review;
    }

    public ProductReview BuildRejected(string reason = "دلیل رد پیش‌فرض")
    {
        var review = Build();
        review.Reject(reason);
        return review;
    }

    public ProductReview BuildDeleted()
    {
        var review = Build();
        review.MarkAsDeleted();
        return review;
    }
}
