namespace Domain.Payment.Results;

public sealed class PaymentSuccessSettlementResult
{
    public bool IsSuccess { get; private set; }
    public bool IsIdempotent { get; private set; }
    public string? Error { get; private set; }

    private PaymentSuccessSettlementResult()
    { }

    public static PaymentSuccessSettlementResult Success() =>
        new() { IsSuccess = true };

    public static PaymentSuccessSettlementResult Idempotent() =>
        new() { IsSuccess = true, IsIdempotent = true };

    public static PaymentSuccessSettlementResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}