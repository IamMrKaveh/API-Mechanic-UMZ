namespace Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string eventType, string action, string details, string? ipAddress = null, string? userAgent = null);
    Task<(IEnumerable<AuditLogDto> Logs, int TotalItems)> GetAuditLogsAsync(int? userId, string? eventType, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task LogProductEventAsync(int productId, string action, string details, int? userId = null);
    Task LogInventoryEventAsync(int productId, string action, string details, int? userId = null);
    Task LogAdminEventAsync(string action, int userId, string details, string? ipAddress = null, string? userAgent = null);
    Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null);
    Task LogOrderEventAsync(int orderId, string action, int userId, string details);
    Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null);
    Task LogSystemEventAsync(string eventType, string details, int? userId = null, string? ipAddress = null, string? userAgent = null);
    Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null);
}