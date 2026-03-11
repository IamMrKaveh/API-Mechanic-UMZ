namespace Domain.Discount.Exceptions;

public sealed class InvalidDiscountException(string message, string? code = null) : DomainException(message)
{
    public string? DiscountCode { get; } = code;
}