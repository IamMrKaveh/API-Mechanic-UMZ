namespace Domain.Discount.Results;

public sealed class DiscountValidation
{
    public bool IsValid { get; }
    public string? FailureReason { get; }

    private DiscountValidation(bool isValid, string? failureReason)
    {
        IsValid = isValid;
        FailureReason = failureReason;
    }

    public static DiscountValidation Success() => new(true, null);

    public static DiscountValidation Fail(string reason) => new(false, reason);
}