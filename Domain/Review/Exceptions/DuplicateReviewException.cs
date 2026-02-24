namespace Domain.Review.Exceptions;

public sealed class DuplicateReviewException : DomainException
{
    public DuplicateReviewException(int userId, int productId)
        : base($"کاربر {userId} قبلاً برای محصول {productId} نظر ثبت کرده است.") { }
}