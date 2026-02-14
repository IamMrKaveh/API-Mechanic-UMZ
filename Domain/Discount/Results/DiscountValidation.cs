namespace Domain.Discount.Results;

public sealed class DiscountValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }

    private DiscountValidation() { }

    public static DiscountValidation Valid() => new() { IsValid = true };

    public static DiscountValidation Invalid(string error) => new() { IsValid = false, Error = error };
}
