namespace Domain.Product.Exceptions;

public class InvalidPriceException : DomainException
{
    public InvalidPriceException(string message) : base(message)
    {
    }
}