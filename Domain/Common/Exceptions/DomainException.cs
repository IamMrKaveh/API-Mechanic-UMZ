namespace Domain.Common.Exceptions;

public abstract class DomainException : Exception
{
    public abstract string ErrorCode { get; }

    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}