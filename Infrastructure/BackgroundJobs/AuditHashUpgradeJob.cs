using Domain.Audit.Entities;

namespace Infrastructure.BackgroundJobs;

public sealed class AuditHashUpgradeJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromHours(6);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogSystemEventAsync(
                    "AuditHashUpgrade",
                    "Audit Hash Upgrade Service started.",
                    ct);
        }

        await Task.Delay(InitialDelay, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:audit-hash-upgrade", LockExpiry, ct);

                if (lockHandle is null || !lockHandle.IsAcquired)
                {
                    await Task.Delay(BatchDelay, ct);
                    continue;
                }

                var processed = await ProcessBatchAsync(ct);

                if (processed == 0)
                    await Task.Delay(IdleDelay, ct);
                else
                    await Task.Delay(BatchDelay, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogErrorAsync(
                        $"AuditHashUpgradeJob error: {ex.GetType().Name}: {ex.Message}",
                        ct);
                await Task.Delay(BatchDelay, ct);
            }
        }

        using var stopScope = scopeFactory.CreateScope();
        await stopScope.ServiceProvider.GetRequiredService<IAuditService>()
            .LogSystemEventAsync(
                "AuditHashUpgrade",
                "Audit Hash Upgrade Service stopped.",
                ct);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var batch = await context.AuditLogs
            .Where(a => a.HashVersion < AuditLog.CurrentHashVersion)
            .OrderBy(a => a.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (batch.Count == 0)
            return 0;

        var upgradedCount = 0;
        foreach (var log in batch)
        {
            log.UpgradeHashVersion();
            upgradedCount++;
        }

        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditHashUpgrade",
            $"Upgraded hash version of {upgradedCount} audit logs to v{AuditLog.CurrentHashVersion} at {dateTimeProvider.UtcNow:O}",
            ct);

        return upgradedCount;
    }
}
