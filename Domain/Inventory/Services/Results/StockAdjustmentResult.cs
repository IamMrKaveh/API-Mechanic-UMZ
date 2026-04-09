namespace Domain.Inventory.Services.Results;

public sealed class StockAdjustmentResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private StockAdjustmentResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static StockAdjustmentResult Success() => new(true);

    public static StockAdjustmentResult Fail(string error) => new(false, error);

    public Result ToResult() => IsSuccess
        ? Result.Success()
        : Result.Failure(new Error("StockAdjustment.Failed", Error ?? string.Empty));
}