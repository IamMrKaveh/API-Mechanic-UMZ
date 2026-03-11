namespace Domain.Review.Exceptions;

public sealed class DuplicateReviewException(int userId, int productId) : DomainException($"کاربر {userId} قبلاً برای محصول {productId} نظر ثبت کرده است.")
{
}