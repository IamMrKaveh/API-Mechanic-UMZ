using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Search;

public sealed class ElasticDeadLetterQueue(
    DBContext context,
    IAuditService auditService) : IElasticDeadLetterQueue
{
    public async Task EnqueueAsync(FailedElasticOperation operation, CancellationToken ct)
    {
        try
        {
            var existing = await context.FailedElasticOperations
                .AnyAsync(o =>
                    o.EntityType == operation.EntityType &&
                    o.EntityId == operation.EntityId &&
                    o.Status == "Pending", ct);

            if (!existing)
            {
                context.FailedElasticOperations.Add(operation);
                await context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Failed to enqueue operation to dead letter queue: {ex.Message}", ct);
        }
    }

    public async Task<IEnumerable<FailedElasticOperation>> DequeueAsync(int count, CancellationToken ct)
    {
        return await context.FailedElasticOperations
            .Where(o => o.Status == "Pending")
            .OrderBy(o => o.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
    }
}