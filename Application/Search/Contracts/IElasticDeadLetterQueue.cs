namespace Application.Search.Contracts;

public interface IElasticDeadLetterQueue
{
    Task EnqueueAsync(FailedIndexOperation operation, CancellationToken ct);

    Task<IEnumerable<FailedIndexOperation>> DequeueAsync(int count, CancellationToken ct);
}