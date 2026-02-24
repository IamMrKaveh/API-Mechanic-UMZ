namespace Domain.Discount.Exceptions;

public sealed class InvalidDiscountException : DomainException
{
    public string? DiscountCode { get; }

    public InvalidDiscountException(string message, string? code = null) : base(message)
    {
        DiscountCode = code;
    }
}