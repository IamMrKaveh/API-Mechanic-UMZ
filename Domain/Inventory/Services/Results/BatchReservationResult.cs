namespace Domain.Inventory.Services.Results;

public sealed class BatchReservationResult
{
    public bool IsSuccess { get; }
    public IReadOnlyList<string> Errors { get; }

    private BatchReservationResult(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static BatchReservationResult Success() => new(true, Array.Empty<string>());

    public static BatchReservationResult Fail(IReadOnlyList<string> errors) => new(false, errors);
}