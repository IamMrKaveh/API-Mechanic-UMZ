namespace Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public AuditService(
        ILogger<AuditService> logger,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer)
    {
        _logger = logger;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        await LogAuditAsync(new Domain.Log.AuditLog
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
        var sanitizedEventType = _htmlSanitizer.Sanitize(eventType);
        var sanitizedDetails = _htmlSanitizer.Sanitize(details);

        await LogAuditAsync(new Domain.Log.AuditLog
        {
            UserId = userId,
            Action = "SecurityEvent",
            Details = sanitizedDetails,
            IpAddress = SanitizeIpAddress(ipAddress),
            Timestamp = DateTime.UtcNow,
            EventType = sanitizedEventType,
            UserAgent = userAgent
        });
        _logger.LogWarning("Security event logged: EventType={EventType}, Details={Details}, IP={IpAddress}, UserId={UserId}",
                    sanitizedEventType, sanitizedDetails, ipAddress, userId);
    }

    public async Task LogOrderEventAsync(int orderId, string action, int userId, string details)
    {
        await LogAuditAsync(new Domain.Log.AuditLog
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
        await LogAuditAsync(new Domain.Log.AuditLog
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
        await LogAuditAsync(new Domain.Log.AuditLog
        {
            UserId = userId,
            Action = _htmlSanitizer.Sanitize(action),
            Details = _htmlSanitizer.Sanitize($"ProductId={productId}, {details}"),
            IpAddress = "system",
            Timestamp = DateTime.UtcNow,
            EventType = "ProductEvent"
        });
    }

    public async Task<(IEnumerable<Domain.Log.AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? userId = null,
        string? eventType = null,
        int page = 1,
        int pageSize = 50)
    {
        return await _auditRepository.GetAuditLogsAsync(fromDate, toDate, userId, eventType, page, pageSize);
    }

    private async Task LogAuditAsync(Domain.Log.AuditLog auditLog)
    {
        try
        {
            await _auditRepository.AddAuditLogAsync(auditLog);
            // Intentionally not calling SaveChangesAsync here to allow batching
            // in an outer transaction (Unit of Work).
            // If out-of-band logging is required, a separate service/method should handle it.

            _logger.LogInformation("Audit prepared: UserId={UserId}, Action={Action}, EventType={EventType}, IP={IpAddress}",
                auditLog.UserId, auditLog.Action, auditLog.EventType, auditLog.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare audit log: Action={Action}, EventType={EventType}", auditLog.Action, auditLog.EventType);
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