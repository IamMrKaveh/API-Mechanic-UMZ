namespace Domain.Inventory.Services.Results;

public sealed class BatchReservationValidation
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    private BatchReservationValidation(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static BatchReservationValidation Valid() => new(true, Array.Empty<string>());

    public static BatchReservationValidation Invalid(IReadOnlyList<string> errors) => new(false, errors);
}