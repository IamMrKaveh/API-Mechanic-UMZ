namespace Domain.Review.Exceptions;

public sealed class ReviewNotFoundException : DomainException
{
    public ReviewNotFoundException(int id)
        : base($"نظری با شناسه {id} یافت نشد.") { }
}