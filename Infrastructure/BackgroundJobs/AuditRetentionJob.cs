using Domain.Audit.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class AuditRetentionJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromHours(2);
    private static readonly int FinancialRetentionDays = 7 * 365;
    private static readonly int SecurityRetentionDays = 2 * 365;
    private static readonly int DefaultRetentionDays = 90;
    private const int DeleteBatchSize = 1000;
    private const int ArchiveBatchSize = 500;

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
        var repository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var archiveStorage = scope.ServiceProvider.GetRequiredService<IAuditArchiveStorage>();
        var now = dateTimeProvider.UtcNow;

        var defaultCutoff = now.AddDays(-DefaultRetentionDays);
        await ArchiveAndDeleteAsync(
            repository, unitOfWork, auditService, archiveStorage, defaultCutoff,
            dateTimeProvider,
            excludeEventTypes: FinancialEventTypes.Union(SecurityEventTypes).ToHashSet(StringComparer.OrdinalIgnoreCase),
            batchLabel: "default", ct: ct);

        var securityCutoff = now.AddDays(-SecurityRetentionDays);
        await ArchiveAndDeleteAsync(
            repository, unitOfWork, auditService, archiveStorage, securityCutoff,
            dateTimeProvider,
            includeEventTypes: SecurityEventTypes,
            excludeEventTypes: FinancialEventTypes,
            batchLabel: "security", ct: ct);

        var financialCutoff = now.AddDays(-FinancialRetentionDays);
        await ArchiveOnlyAsync(
            repository, unitOfWork, auditService, archiveStorage, financialCutoff,
            dateTimeProvider,
            includeEventTypes: FinancialEventTypes,
            batchLabel: "financial", ct: ct);

        await auditService.LogSystemEventAsync("AuditRetention", "Retention cycle completed.", ct);
    }

    private static async Task ArchiveAndDeleteAsync(
        IAuditRepository repository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IAuditArchiveStorage archiveStorage,
        DateTime cutoff,
        IDateTimeProvider dateTimeProvider,
        HashSet<string>? includeEventTypes = null,
        HashSet<string>? excludeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var logsToArchive = await repository.GetForArchiveAsync(
            cutoff,
            includeEventTypes,
            excludeEventTypes,
            onlyNonArchived: false,
            batchSize: DeleteBatchSize,
            ct: ct);

        if (logsToArchive.Count == 0) return;

        await archiveStorage.ArchiveAsync(logsToArchive, batchLabel, dateTimeProvider.UtcNow, ct);

        await repository.RemoveRangeAsync(logsToArchive, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived and deleted {logsToArchive.Count} {batchLabel} audit logs older than {cutoff}",
            ct);
    }

    private static async Task ArchiveOnlyAsync(
        IAuditRepository repository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IAuditArchiveStorage archiveStorage,
        DateTime cutoff,
        IDateTimeProvider dateTimeProvider,
        HashSet<string>? includeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var logsToArchive = await repository.GetForArchiveAsync(
            cutoff,
            includeEventTypes,
            excludeEventTypes: null,
            onlyNonArchived: true,
            batchSize: ArchiveBatchSize,
            ct: ct);

        if (logsToArchive.Count == 0) return;

        await archiveStorage.ArchiveAsync(logsToArchive, batchLabel, dateTimeProvider.UtcNow, ct);

        foreach (var log in logsToArchive)
            log.MarkAsArchived();

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived {logsToArchive.Count} {batchLabel} audit logs (preserved in DB).",
            ct);
    }
}
