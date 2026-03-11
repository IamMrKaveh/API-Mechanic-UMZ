namespace Domain.Inventory.Services.Results;

public sealed class BatchReleaseResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private BatchReleaseResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static BatchReleaseResult Success() => new(true);

    public static BatchReleaseResult Fail(string error) => new(false, error);
}