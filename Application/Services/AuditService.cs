namespace Application.Services;

public class AuditService : IAuditService
{
    private readonly LedkaContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(LedkaContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(int? userId, string eventType, string action, string details, string? ipAddress = null, string? userAgent = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var log = new AuditLog
        {
            UserId = userId,
            EventType = eventType,
            Action = action,
            Details = details,
            IpAddress = ipAddress ?? httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = userAgent ?? httpContext?.Request?.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<AuditLogDto> Logs, int TotalItems)> GetAuditLogsAsync(int? userId, string? eventType, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(l => l.EventType == eventType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= toDate.Value);
        }

        var totalItems = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                EventType = l.EventType,
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                Timestamp = l.Timestamp
            })
            .ToListAsync();

        return (logs, totalItems);
    }

    public async Task LogProductEventAsync(int productId, string action, string details, int? userId = null)
    {
        await LogAsync(userId, "ProductEvent", action, $"ProductId={productId}, {details}");
    }

    public async Task LogInventoryEventAsync(int productId, string action, string details, int? userId = null)
    {
        await LogAsync(userId, "InventoryEvent", action, $"Inventory: ProductId={productId}, {details}");
    }

    public async Task LogAdminEventAsync(string action, int userId, string details, string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(userId, "AdminEvent", action, details, ipAddress, userAgent);
    }

    public async Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        await LogAsync(userId, "CartEvent", action, details, ipAddress, userAgent);
    }

    public async Task LogOrderEventAsync(int orderId, string action, int userId, string details)
    {
        await LogAsync(userId, "OrderEvent", action, $"OrderId={orderId}, {details}");
    }

    public async Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null)
    {
        await LogAsync(userId, eventType, "SecurityEvent", details, ipAddress, userAgent);
    }

    public async Task LogSystemEventAsync(string eventType, string details, int? userId = null, string? ipAddress = null, string? userAgent = null)
    {
        await LogAsync(userId, eventType, "SystemEvent", details, ipAddress ?? "system", userAgent);
    }

    public async Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
    {
        await LogAsync(userId, "UserAction", action, details, ipAddress, userAgent);
    }
}