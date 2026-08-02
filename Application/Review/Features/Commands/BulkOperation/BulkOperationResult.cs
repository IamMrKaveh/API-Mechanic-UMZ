namespace Application.Review.Features.Commands.BulkOperation;

public sealed record BulkOperationResult(
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<Guid> FailedIds,
    IReadOnlyList<BulkOperationFailure> Failures);

public sealed record BulkOperationFailure(
    Guid ReviewId,
    string Error);
