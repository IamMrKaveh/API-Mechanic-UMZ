namespace SharedKernel.Exceptions;

public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string? ErrorCode { get; }

    public ExternalServiceException(string serviceName, string message)
        : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, string? errorCode)
        : base(message)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, string? errorCode, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
    }
}