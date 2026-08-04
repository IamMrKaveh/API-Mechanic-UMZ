namespace SharedKernel.Exceptions;

public class DomainException(
    string errorCode,
    string message,
    IReadOnlyDictionary<string, object?>? args,
    Exception? innerException) : Exception(message, innerException)
{
    public virtual string ErrorCode { get; } = string.IsNullOrWhiteSpace(errorCode) ? "DOMAIN_ERROR" : errorCode;

    public IReadOnlyDictionary<string, object?> Args { get; } = args ?? new Dictionary<string, object?>();

    public DomainException(string message)
        : this("DOMAIN_ERROR", message, args: null, innerException: null)
    {
    }

    public DomainException(string errorCode, string message)
        : this(errorCode, message, args: null, innerException: null)
    {
    }

    public DomainException(string errorCode, string message, IReadOnlyDictionary<string, object?>? args)
        : this(errorCode, message, args, innerException: null)
    {
    }

    public DomainException(string message, Exception innerException)
        : this("DOMAIN_ERROR", message, args: null, innerException)
    {
    }
}
