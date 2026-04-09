using Domain.Review.ValueObjects;

namespace Domain.Review.Exceptions;

public sealed class ReviewNotFoundException : DomainException
{
    public ReviewId ReviewId { get; }

    public override string ErrorCode => "REVIEW_NOT_FOUND";

    public ReviewNotFoundException(ReviewId id)
        : base($"نظری با شناسه {id} یافت نشد.")
    {
        ReviewId = id;
    }
}