namespace Domain.Common.Exceptions;

public class DomainException : Exception
{
    public virtual string ErrorCode => "DOMAIN_ERROR";

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}