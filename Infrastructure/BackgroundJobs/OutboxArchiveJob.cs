using Infrastructure.BackgroundJobs.Common;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.BackgroundJobs;

public sealed class OutboxArchiveJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxArchiveJob> logger) : DistributedLockedBackgroundService(scopeFactory, logger)
{
    private const int RetentionDays = 30;
    private const int BatchSize = 500;

    protected override string LockKey => "jobs:outbox-archive";
    protected override TimeSpan Interval => TimeSpan.FromHours(6);
    protected override TimeSpan LockExpiry => TimeSpan.FromMinutes(15);

    protected override async Task ExecuteInsideLockAsync(IServiceProvider services, CancellationToken ct)
    {
        var context = services.GetRequiredService<DBContext>();
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var totalArchived = 0;

            while (!ct.IsCancellationRequested)
            {
                await using var transaction = await context.Database.BeginTransactionAsync(ct);

                var batch = await context.OutboxMessages
                    .Where(m => m.ProcessedAt != null
                                && m.ProcessedAt < cutoff
                                && !m.IsPoisoned)
                    .OrderBy(m => m.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0)
                {
                    await transaction.RollbackAsync(ct);
                    break;
                }

                var archivedAt = DateTime.UtcNow;
                var archiveRows = batch
                    .Select(m => OutboxArchiveMessage.FromProcessed(m, archivedAt))
                    .ToList();

                await context.OutboxArchiveMessages.AddRangeAsync(archiveRows, ct);
                context.OutboxMessages.RemoveRange(batch);

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                totalArchived += batch.Count;

                if (batch.Count < BatchSize)
                    break;
            }

            if (totalArchived > 0)
                logger.LogInformation(
                    "OutboxArchiveJob archived {Count} messages older than {Days} days.",
                    totalArchived,
                    RetentionDays,
                    ct);
        });
    }
}
