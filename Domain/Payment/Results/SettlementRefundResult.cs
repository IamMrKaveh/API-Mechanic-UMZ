namespace Domain.Payment.Results;

public sealed class SettlementRefundResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public decimal? RefundedAmount { get; private set; }

    private SettlementRefundResult()
    { }

    public static SettlementRefundResult Success(decimal amount) =>
        new() { IsSuccess = true, RefundedAmount = amount };

    public static SettlementRefundResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}