using Application.Search.Features.Shared;

namespace Infrastructure.Search;

public sealed class ElasticDeadLetterQueue(DBContext context) : IElasticDeadLetterQueue
{
    public async Task<IEnumerable<FailedElasticOperation>> DequeueAsync(int count, CancellationToken ct)
    {
        return await context.FailedElasticOperations
            .Where(o => o.Status == "Pending")
            .OrderBy(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}