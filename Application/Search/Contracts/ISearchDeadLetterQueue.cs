using Application.Search.Features.Shared;

namespace Application.Search.Contracts;

public interface ISearchDeadLetterQueue
{
    Task EnqueueAsync(FailedElasticOperation operation, CancellationToken ct);

    Task<IEnumerable<FailedElasticOperation>> DequeueAsync(int count, CancellationToken ct);
}