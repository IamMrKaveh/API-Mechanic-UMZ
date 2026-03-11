namespace Domain.Order.Results;

public sealed class OrderItemsValidation(bool isValid, IEnumerable<string> errors)
{
    public bool IsValid { get; } = isValid;
    public IReadOnlyList<string> Errors { get; } = errors.ToList().AsReadOnly();

    public string GetErrorsSummary() => string.Join(" ", Errors);
}