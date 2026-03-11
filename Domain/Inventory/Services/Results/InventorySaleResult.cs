namespace Domain.Inventory.Services.Results;

public sealed class InventorySaleResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private InventorySaleResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static InventorySaleResult Success() => new(true);

    public static InventorySaleResult Fail(string error) => new(false, error);
}