namespace Infrastructure.Audit.BackgroundServices;

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
public sealed class AuditRetentionService : BackgroundService
{
    // هر روز یک بار در ساعت 2 بامداد اجرا می‌شود
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    // سیاست‌های Retention
    private static readonly int FinancialRetentionDays = 7 * 365; // 7 سال

    private static readonly int SecurityRetentionDays = 2 * 365; // 2 سال
    private static readonly int DefaultRetentionDays = 90;       // 90 روز

    // انواع رویداد مالی
    private static readonly HashSet<string> FinancialEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PaymentEvent", "OrderEvent", "RefundEvent", "FinancialEvent", "TransactionEvent"
    };

    // انواع رویداد امنیتی
    private static readonly HashSet<string> SecurityEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SecurityEvent", "UserAction", "AdminEvent", "AuthEvent", "LoginEvent"
    };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditRetentionService> _logger;

    public AuditRetentionService(
        IServiceProvider serviceProvider,
        ILogger<AuditRetentionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit Retention Service started.");

        // تأخیر اولیه تا سیستم کاملاً بالا بیاید
        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit retention process.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("Audit Retention Service stopped.");
    }

    private async Task RunRetentionAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LedkaContext>();
        var archiveFolder = GetArchiveFolder();

        var now = DateTime.UtcNow;

        // ─── 1. بررسی لاگ‌های عادی (90 روز) ────────────────────
        var defaultCutoff = now.AddDays(-DefaultRetentionDays);

        await ArchiveAndDeleteAsync(
            context,
            archiveFolder,
            defaultCutoff,
            FinancialEventTypes.Union(SecurityEventTypes).ToHashSet(),
            batchLabel: "default",
            ct: ct);

        // ─── 2. بررسی لاگ‌های امنیتی (2 سال) ───────────────────
        var securityCutoff = now.AddDays(-SecurityRetentionDays);
        await ArchiveAndDeleteAsync(context, archiveFolder,
            securityCutoff,
            includeEventTypes: SecurityEventTypes,
            excludeEventTypes: FinancialEventTypes,
            batchLabel: "security",
            ct);

        // ─── 3. لاگ‌های مالی هرگز حذف نمی‌شوند - فقط Archive ──
        // پس از 7 سال به Archive File منتقل می‌شوند
        var financialCutoff = now.AddDays(-FinancialRetentionDays);
        await ArchiveOnlyAsync(context, archiveFolder,
            financialCutoff,
            includeEventTypes: FinancialEventTypes,
            batchLabel: "financial",
            ct);

        _logger.LogInformation("[AuditRetention] Retention cycle completed.");
    }

    private async Task ArchiveAndDeleteAsync(
        LedkaContext context,
        string archiveFolder,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        HashSet<string>? excludeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.Timestamp < cutoff);

        if (includeEventTypes?.Any() == true)
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        if (excludeEventTypes?.Any() == true)
            query = query.Where(a => !excludeEventTypes.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.Timestamp)
            .Take(1000) // در هر اجرا حداکثر 1000 رکورد
            .ToListAsync(ct);

        if (!logsToArchive.Any()) return;

        // Export به فایل JSON
        await ExportToArchiveFileAsync(archiveFolder, logsToArchive, batchLabel, ct);

        // حذف از جدول اصلی
        context.AuditLogs.RemoveRange(logsToArchive);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[AuditRetention] Archived and deleted {Count} {Label} audit logs older than {Cutoff:yyyy-MM-dd}",
            logsToArchive.Count, batchLabel, cutoff);
    }

    private async Task ArchiveOnlyAsync(
        LedkaContext context,
        string archiveFolder,
        DateTime cutoff,
        HashSet<string>? includeEventTypes = null,
        string batchLabel = "",
        CancellationToken ct = default)
    {
        var query = context.AuditLogs
            .Where(a => a.Timestamp < cutoff && !a.IsArchived);

        if (includeEventTypes?.Any() == true)
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        var logsToArchive = await query
            .OrderBy(a => a.Timestamp)
            .Take(500)
            .ToListAsync(ct);

        if (!logsToArchive.Any()) return;

        await ExportToArchiveFileAsync(archiveFolder, logsToArchive, batchLabel, ct);

        // علامت‌گذاری به جای حذف (قانونی)
        foreach (var log in logsToArchive)
            log.MarkAsArchived();

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[AuditRetention] Archived {Count} {Label} audit logs (preserved in DB).",
            logsToArchive.Count, batchLabel);
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