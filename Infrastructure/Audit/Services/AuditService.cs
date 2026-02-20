namespace Infrastructure.Audit.Services;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        ILogger<AuditService> logger,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(int? userId, string eventType, string action, string details, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = AuditLog.Create(
                            userId,
                            SanitizeInput(eventType),
                            SanitizeInput(action),
                            SanitizeInput(details),
                            SanitizeIpAddress(ipAddress ?? httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown"),
                            SanitizeUserAgent(userAgent ?? httpContext?.Request?.Headers["User-Agent"].ToString())
                        );

            await _auditRepository.AddAuditLogAsync(auditLog);

            _logger.LogInformation(
                "Audit log: UserId={UserId}, EventType={EventType}, Action={Action}",
                userId, eventType, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action: {Action}", action);
        }
    }

    public async Task<(IEnumerable<AuditDtos> Logs, int TotalItems)> GetAuditLogsAsync(
        int? userId, string? eventType, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var (logs, totalCount) = await _auditRepository.GetAuditLogsAsync(fromDate, toDate, userId, eventType, page, pageSize);

        var dtos = logs.Select(l => new AuditDtos
        {
            Id = l.Id,
            UserId = l.UserId,
            EventType = l.EventType,
            Action = l.Action,
            Details = l.Details,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            Timestamp = l.Timestamp
        });

        return (dtos, totalCount);
    }

    public Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        return LogAsync(userId, "UserAction", action, details, ipAddress, userAgent);
    }

    public Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null)
    {
        _logger.LogWarning("Security event: EventType={EventType}, Details={Details}, IP={IpAddress}",
            eventType, details, ipAddress);
        return LogAsync(userId, eventType, "SecurityEvent", details, ipAddress, userAgent);
    }

    public Task LogSystemEventAsync(string eventType, string details, int? userId = null, string? ipAddress = null, string? userAgent = null)
    {
        return LogAsync(userId, eventType, "SystemEvent", details, ipAddress ?? "system", userAgent);
    }

    public Task LogAdminEventAsync(string action, int userId, string details, string? ipAddress = null, string? userAgent = null)
    {
        return LogAsync(userId, "AdminEvent", action, details, ipAddress ?? "system", userAgent);
    }

    public Task LogOrderEventAsync(int orderId, string action, int userId, string details)
    {
        return LogAsync(userId, "OrderEvent", action, $"OrderId={orderId}, {details}");
    }

    public Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        return LogAsync(userId, "CartEvent", action, details, ipAddress, userAgent);
    }

    public Task LogProductEventAsync(int productId, string action, string details, int? userId = null)
    {
        return LogAsync(userId, "ProductEvent", action, $"ProductId={productId}, {details}");
    }

    public Task LogInventoryEventAsync(int productId, string action, string details, int? userId = null)
    {
        return LogAsync(userId, "InventoryEvent", action, $"Inventory: ProductId={productId}, {details}");
    }

    public Task<byte[]> ExportToCsvAsync(AuditExportRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    private string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return _htmlSanitizer.Sanitize(input);
    }

    private string SanitizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return "unknown";
        if (ipAddress.Length > 45) return ipAddress.Substring(0, 45);
        return ipAddress;
    }

    private string? SanitizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        if (userAgent.Length > 500) return userAgent.Substring(0, 500);
        return userAgent;
    }
}