using Application.Search.Features.Shared;

namespace Application.Search.Contracts;

public interface IElasticDeadLetterQueue
{
    Task<IEnumerable<FailedElasticOperation>> DequeueAsync(
        int count,
        CancellationToken ct);
}