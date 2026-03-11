namespace Domain.Payment.Results;

public sealed class RefundEligibilityResult
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public decimal? EligibleAmount { get; private set; }

    private RefundEligibilityResult()
    { }

    public static RefundEligibilityResult Success(decimal amount) =>
        new() { IsValid = true, EligibleAmount = amount };

    public static RefundEligibilityResult Failed(string error) =>
        new() { IsValid = false, Error = error };
}