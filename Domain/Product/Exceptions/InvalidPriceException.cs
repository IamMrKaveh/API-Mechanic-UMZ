namespace Domain.Product.Exceptions;

public sealed class InvalidPriceException(string message) : DomainException(message)
{
    public override string ErrorCode => "INVALID_PRICE";
}