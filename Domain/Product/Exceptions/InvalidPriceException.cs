namespace Domain.Product.Exceptions;

public class InvalidPriceException(string message) : DomainException(message)
{
}