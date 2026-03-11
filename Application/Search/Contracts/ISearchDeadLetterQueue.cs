namespace Application.Search.Contracts;

public interface ISearchDeadLetterQueue
{
    Task EnqueueAsync(FailedIndexOperation operation, CancellationToken ct);

    Task<IEnumerable<FailedIndexOperation>> DequeueAsync(int count, CancellationToken ct);
}