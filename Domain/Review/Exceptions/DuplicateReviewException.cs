using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Exceptions;

public sealed class DuplicateReviewException : DomainException
{
    public UserId UserId { get; }
    public ProductId ProductId { get; }

    public override string ErrorCode => "DUPLICATE_REVIEW";

    public DuplicateReviewException(UserId userId, ProductId productId)
        : base($"کاربر {userId} قبلاً برای محصول {productId} نظر ثبت کرده است.")
    {
        UserId = userId;
        ProductId = productId;
    }
}