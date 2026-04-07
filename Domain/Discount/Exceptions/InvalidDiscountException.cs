using Domain.Common.Exceptions;

namespace Domain.Discount.Exceptions;

public sealed class InvalidDiscountException : DomainException
{
    public string? DiscountCode { get; }

    public override string ErrorCode => "INVALID_DISCOUNT";

    public InvalidDiscountException(string message, string? code = null)
        : base(message)
    {
        DiscountCode = code;
    }
}