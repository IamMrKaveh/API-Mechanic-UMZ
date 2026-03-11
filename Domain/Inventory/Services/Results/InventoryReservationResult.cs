namespace Domain.Inventory.Services.Results;

public sealed class InventoryReservationResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private InventoryReservationResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static InventoryReservationResult Success() => new(true);

    public static InventoryReservationResult Fail(string error) => new(false, error);
}