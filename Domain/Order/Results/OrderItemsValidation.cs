namespace Domain.Order.Results;

public sealed class OrderItemsValidation
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public OrderItemsValidation(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }

    public string GetErrorsSummary() => string.Join(" ", Errors);
}