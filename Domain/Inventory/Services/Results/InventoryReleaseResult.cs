namespace Domain.Inventory.Services.Results;

public sealed class InventoryReleaseResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private InventoryReleaseResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static InventoryReleaseResult Success() => new(true);

    public static InventoryReleaseResult Fail(string error) => new(false, error);
}