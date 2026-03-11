namespace Domain.Payment.Results;

public sealed class PaymentProcessResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public long? RefId { get; private set; }

    private PaymentProcessResult()
    { }

    public static PaymentProcessResult Success(long refId)
    {
        return new PaymentProcessResult { IsSuccess = true, RefId = refId };
    }

    public static PaymentProcessResult Failed(string error)
    {
        return new PaymentProcessResult { IsSuccess = false, Error = error };
    }
}