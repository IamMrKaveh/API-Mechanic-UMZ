namespace Application.Audit.Contracts;

/// <summary>
/// سرویس ثبت لاگ‌های حسابرسی
/// </summary>
public interface IAuditService
{
    Task LogAsync(int? userId, string eventType, string action, string details, string? ipAddress = null, string? userAgent = null);

    Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null);

    Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null);

    Task LogSystemEventAsync(string eventType, string details, int? userId = null, string? ipAddress = null, string? userAgent = null);

    Task LogAdminEventAsync(string action, int userId, string details, string? ipAddress = null, string? userAgent = null);

    Task LogOrderEventAsync(int orderId, string action, int userId, string details);

    Task LogProductEventAsync(int productId, string action, string details, int? userId = null);

    Task LogInventoryEventAsync(int productId, string action, string details, int? userId = null);

    /// <summary>
    /// دریافت لاگ‌های حسابرسی با فیلتر و صفحه‌بندی
    /// </summary>
    Task<(IEnumerable<AuditDtos> Logs, int TotalItems)> GetAuditLogsAsync(
        int? userId, string? eventType, DateTime? fromDate, DateTime? toDate, int page, int pageSize);

    Task<byte[]> ExportToCsvAsync(AuditExportRequest request, CancellationToken ct = default);
}