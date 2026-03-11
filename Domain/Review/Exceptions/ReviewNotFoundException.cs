namespace Domain.Review.Exceptions;

public sealed class ReviewNotFoundException : DomainException
{
    public ReviewNotFoundException(ProductReviewId id) : base($"نظری با شناسه {id.Value} یافت نشد.")
    {
    }

    public ReviewNotFoundException(int id) : base($"نظری با شناسه {id} یافت نشد.")
    {
    }
}