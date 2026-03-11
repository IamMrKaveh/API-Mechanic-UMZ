namespace Domain.Inventory.Services.Results;

public sealed class StockValidationResult
{
    public bool IsValid { get; }
    public string? Error { get; }
    public int? AvailableStock { get; }
    public int? RequestedQuantity { get; }

    private StockValidationResult(bool isValid, string? error = null, int? availableStock = null, int? requestedQuantity = null)
    {
        IsValid = isValid;
        Error = error;
        AvailableStock = availableStock;
        RequestedQuantity = requestedQuantity;
    }

    public static StockValidationResult Valid() => new(true);

    public static StockValidationResult Invalid(string error) => new(false, error);

    public static StockValidationResult InsufficientStock(int available, int requested)
        => new(false, $"موجودی کافی نیست. موجودی: {available}، درخواستی: {requested}", available, requested);
}