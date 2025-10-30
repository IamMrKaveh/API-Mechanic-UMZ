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

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly MechanicContext _context;
    private readonly IHtmlSanitizer _htmlSanitizer;
    public AuditService(ILogger<AuditService> logger, MechanicContext context, IHtmlSanitizer htmlSanitizer)
    {
        _logger = logger;
        _context = context;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        await LogAuditAsync(new TAuditLogs
        {
            UserId = userId,
            Action = _htmlSanitizer.Sanitize(action),
            Details = _htmlSanitizer.Sanitize(details),
            IpAddress = SanitizeIpAddress(ipAddress),
            Timestamp = DateTime.UtcNow,
            EventType = "UserAction",
            UserAgent = userAgent
        });
    }

    public async Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null)
    {
        await LogAuditAsync(new TAuditLogs
        {
            UserId = userId,
            Action = "SecurityEvent",
            Details = _htmlSanitizer.Sanitize(details),
            IpAddress = SanitizeIpAddress(ipAddress),
            Timestamp = DateTime.UtcNow,
            EventType = _htmlSanitizer.Sanitize(eventType),
            UserAgent = userAgent
        });
        _logger.LogWarning("Security event logged: EventType={EventType}, Details={Details}, IP={IpAddress}, UserId={UserId}",
                    eventType, details, ipAddress, userId);
    }

    public async Task LogOrderEventAsync(int orderId, string action, int userId, string details)
    {
        await LogAuditAsync(new TAuditLogs
        {
            UserId = userId,
            Action = _htmlSanitizer.Sanitize(action),
            Details = _htmlSanitizer.Sanitize($"OrderId={orderId}, {details}"),
            IpAddress = "system",
            Timestamp = DateTime.UtcNow,
            EventType = "OrderEvent"
        });
    }

    public async Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        await LogAuditAsync(new TAuditLogs
        {
            UserId = userId,
            Action = _htmlSanitizer.Sanitize(action),
            Details = _htmlSanitizer.Sanitize(details),
            IpAddress = SanitizeIpAddress(ipAddress),
            Timestamp = DateTime.UtcNow,
            EventType = "CartEvent",
            UserAgent = userAgent
        });
    }

    public async Task LogProductEventAsync(int productId, string action, string details, int? userId = null)
    {
        await LogAuditAsync(new TAuditLogs
        {
            UserId = userId,
            Action = _htmlSanitizer.Sanitize(action),
            Details = _htmlSanitizer.Sanitize($"ProductId={productId}, {details}"),
            IpAddress = "system",
            Timestamp = DateTime.UtcNow,
            EventType = "ProductEvent"
        });
    }

    public async Task<(IEnumerable<TAuditLogs> Logs, int TotalCount)> GetAuditLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? userId = null,
        string? eventType = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.TAuditLogs.AsNoTracking().AsQueryable();
        if (fromDate.HasValue)
            query = query.Where(log => log.Timestamp >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(log => log.Timestamp <= toDate.Value);
        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(log => log.EventType == eventType);

        var totalCount = await query.CountAsync();

        var logs = await query
                    .OrderByDescending(log => log.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

        return (logs, totalCount);
    }

    private async Task LogAuditAsync(TAuditLogs auditLog)
    {
        try
        {
            _context.TAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit logged: UserId={UserId}, Action={Action}, EventType={EventType}, IP={IpAddress}",
                auditLog.UserId, auditLog.Action, auditLog.EventType, auditLog.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit: Action={Action}, EventType={EventType}", auditLog.Action, auditLog.EventType);
        }
    }

    private string SanitizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return "unknown";
        if (ipAddress.Length > 45)
            return ipAddress.Substring(0, 45);

        return ipAddress;
    }
}