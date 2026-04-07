using Domain.Common.Exceptions;

namespace Domain.Product.Exceptions;

public sealed class InvalidPriceException : DomainException
{
    public override string ErrorCode => "INVALID_PRICE";

    public InvalidPriceException(string message)
        : base(message)
    {
    }
}