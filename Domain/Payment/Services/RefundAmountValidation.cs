namespace Domain.Payment.Services;

public sealed class RefundAmountValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public decimal RefundAmount { get; private set; }

    private RefundAmountValidation()
    { }

    public static RefundAmountValidation Success(decimal amount) =>
        new() { IsValid = true, RefundAmount = amount };

    public static RefundAmountValidation Failed(string error) =>
        new() { IsValid = false, Error = error };
}