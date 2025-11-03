namespace MainApi.Services.Audit;

public interface IAuditService
{
    Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null);
    Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null);
    Task LogOrderEventAsync(int orderId, string action, int userId, string details);
    Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null);
    Task LogProductEventAsync(int productId, string action, string details, int? userId = null);
    Task<(IEnumerable<TAuditLogs> Logs, int TotalCount)> GetAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? userId = null, string? eventType = null, int page = 1, int pageSize = 50);
}
