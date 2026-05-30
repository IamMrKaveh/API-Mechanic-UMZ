using Infrastructure.BackgroundJobs.Abstractions;

namespace Infrastructure.BackgroundJobs;

public sealed class AuditRetentionJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromHours(2);
    private static readonly int FinancialRetentionDays = 7 * 365;
    private static readonly int SecurityRetentionDays = 2 * 365;
    private static readonly int DefaultRetentionDays = 90;

    private static readonly HashSet<string> FinancialEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PaymentEvent", "OrderEvent", "RefundEvent", "FinancialEvent", "TransactionEvent"
    };

    private static readonly HashSet<string> SecurityEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SecurityEvent", "UserAction", "AdminEvent", "AuthEvent", "LoginEvent"
    };

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogSystemEventAsync("Audit Retention", "Audit Retention Service started.", ct);
        }

        await Task.Delay(InitialDelay, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:audit-retention", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await RunRetentionAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogSystemEventAsync(ex.Message, "Error during audit retention process.", ct);
            }

            await Task.Delay(CheckInterval, ct);
        }

        using var stopScope = scopeFactory.CreateScope();
        await stopScope.ServiceProvider.GetRequiredService<IAuditService>()
            .LogSystemEventAsync("Audit Retention", "Audit Retention Service stopped.", ct);
    }

    private async Task RunRetentionAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var archiveStorage = scope.ServiceProvider.GetRequiredService<IAuditArchiveStorage>();
        var now = DateTime.UtcNow;

        var defaultCutoff = now.AddDays(-DefaultRetentionDays);
        await ArchiveAndDeleteAsync(
            context, auditService, archiveStorage, defaultCutoff,
            FinancialEventTypes.Union(SecurityEventTypes).ToHashSet(),
            batchLabel: "default", ct: ct);

        var securityCutoff = now.AddDays(-SecurityRetentionDays);
        await ArchiveAndDeleteAsync(
            context, auditService, archiveStorage, securityCutoff,
            includeEventTypes: SecurityEventTypes,
            excludeEventTypes: FinancialEventTypes,
            batchLabel: "security", ct: ct);

        var financialCutoff = now.AddDays(-FinancialRetentionDays);
        await ArchiveOnlyAsync(
            context, auditService, archiveStorage, financialCutoff,
            includeEventTypes: FinancialEventTypes,
            batchLabel: "financial", ct: ct);

        await auditService.LogSystemEventAsync("AuditRetention", "Retention cycle completed.", ct);
    }

    private static async Task ArchiveAndDeleteAsync(
        DBContext context,
        IAuditService auditService,
        IAuditArchiveStorage archiveStorage,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        HashSet<string>? excludeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.Where(a => a.CreatedAt < cutoff);

        if (includeEventTypes?.Count != 0)
            query = query.Where(a => includeEventTypes!.Contains(a.EventType));

        if (excludeEventTypes?.Count != 0)
            query = query.Where(a => !excludeEventTypes!.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.CreatedAt)
            .Take(1000)
            .ToListAsync(ct);

        if (logsToArchive.Count == 0) return;

        await archiveStorage.ArchiveAsync(logsToArchive, batchLabel, DateTime.UtcNow, ct);

        context.AuditLogs.RemoveRange(logsToArchive);
        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived and deleted {logsToArchive.Count} {batchLabel} audit logs older than {cutoff}",
            ct);
    }

    private static async Task ArchiveOnlyAsync(
        DBContext context,
        IAuditService auditService,
        IAuditArchiveStorage archiveStorage,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs.Where(a => a.CreatedAt < cutoff && !a.IsArchived);

        if (includeEventTypes?.Any() == true)
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        if (logsToArchive.Count == 0) return;

        await archiveStorage.ArchiveAsync(logsToArchive, batchLabel, DateTime.UtcNow, ct);

        foreach (var log in logsToArchive)
            log.MarkAsArchived();

        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived {logsToArchive.Count} {batchLabel} audit logs (preserved in DB).",
            ct);
    }
}