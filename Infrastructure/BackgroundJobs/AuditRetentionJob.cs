using Domain.Audit.Entities;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// سرویس Retention لاگ‌های حسابرسی.
///
/// سیاست‌ها:
/// - لاگ‌های مالی: 7 سال نگه‌داری (الزام قانونی)
/// - لاگ‌های امنیتی: 2 سال نگه‌داری
/// - لاگ‌های معمولی: 90 روز نگه‌داری
///
/// فرایند:
/// 1. لاگ‌های منقضی را به جدول Archive منتقل می‌کند
/// 2. یا به فایل JSON Export می‌کند
/// 3. از جدول اصلی حذف می‌کند (برای عملکرد)
/// </summary>
public sealed class AuditRetentionJob(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

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
        await auditService.LogSystemEventAsync(
            "Audit Retention",
            "Audit Retention Service started.",
            ct);

        await Task.Delay(InitialDelay, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunRetentionAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogSystemEventAsync(
                    ex.Message,
                    "Error during audit retention process.",
                    ct);
            }

            await Task.Delay(CheckInterval, ct);
        }

        await auditService.LogSystemEventAsync(
            "Audit Retention",
            "Audit Retention Service stopped.",
            ct);
    }

    private async Task RunRetentionAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();
        var archiveFolder = GetArchiveFolder();

        var now = DateTime.UtcNow;

        var defaultCutoff = now.AddDays(-DefaultRetentionDays);

        await ArchiveAndDeleteAsync(
            context,
            archiveFolder,
            defaultCutoff,
            FinancialEventTypes.Union(SecurityEventTypes).ToHashSet(),
            batchLabel: "default",
            ct: ct);

        var securityCutoff = now.AddDays(-SecurityRetentionDays);
        await ArchiveAndDeleteAsync(context, archiveFolder,
            securityCutoff,
            includeEventTypes: SecurityEventTypes,
            excludeEventTypes: FinancialEventTypes,
            batchLabel: "security",
            ct);

        var financialCutoff = now.AddDays(-FinancialRetentionDays);
        await ArchiveOnlyAsync(context, archiveFolder,
            financialCutoff,
            includeEventTypes: FinancialEventTypes,
            batchLabel: "financial",
            ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            "Retention cycle completed.",
            ct);
    }

    private async Task ArchiveAndDeleteAsync(
        Persistence.Context.DBContext context,
        string archiveFolder,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        HashSet<string>? excludeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.CreatedAt < cutoff);

        if (includeEventTypes?.Count != 0)
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        if (excludeEventTypes?.Count != 0)
            query = query.Where(a => !excludeEventTypes.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.CreatedAt)
            .Take(1000)
            .ToListAsync(ct);

        if (logsToArchive.Count != 0) return;

        await ExportToArchiveFileAsync(archiveFolder, logsToArchive, batchLabel, ct);

        context.AuditLogs.RemoveRange(logsToArchive);
        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived and deleted {logsToArchive.Count} {batchLabel} audit logs older than {cutoff}",
            ct);
    }

    private async Task ArchiveOnlyAsync(
        Persistence.Context.DBContext context,
        string archiveFolder,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.CreatedAt < cutoff && !a.IsArchived);

        if (includeEventTypes?.Any() == true)
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        if (logsToArchive.Count == 0) return;

        await ExportToArchiveFileAsync(archiveFolder, logsToArchive, batchLabel, ct);

        foreach (var log in logsToArchive)
            log.MarkAsArchived();

        await context.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "AuditRetention",
            $"Archived {logsToArchive.Count} {batchLabel} audit logs (preserved in DB).",
            ct);
    }

    private static async Task ExportToArchiveFileAsync(
        string archiveFolder,
        IEnumerable<AuditLog> logs,
        string label,
        CancellationToken ct)
    {
        var date = DateTime.UtcNow;
        var fileName = $"audit_{label}_{date:yyyy-MM-dd_HH}_{Guid.NewGuid():N[..8]}.json";
        var filePath = Path.Combine(archiveFolder, date.Year.ToString(), fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json, ct);
    }

    private static string GetArchiveFolder()
    {
        var folder = Environment.GetEnvironmentVariable("AUDIT_ARCHIVE_PATH")
                     ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit_archives");
        Directory.CreateDirectory(folder);
        return folder;
    }
}