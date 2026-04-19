namespace Application.Common.Interfaces;

public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken ct = default);

    Task MarkAsProcessedAsync(Guid idempotencyKey, string result, CancellationToken ct = default);

    Task<string?> GetResultAsync(Guid idempotencyKey, CancellationToken ct = default);
}