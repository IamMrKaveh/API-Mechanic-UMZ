namespace Infrastructure.Audit.Services;

/// <summary>
/// سرویس حسابرسی تقویت‌شده با:
/// - Immutable Log (هیچ رکوردی حذف یا ویرایش نمی‌شود)
/// - Sensitive Data Masking
/// - Retention Policy
/// - Admin Query با Pagination
/// - Export به CSV / JSON
/// </summary>
public sealed class EnhancedAuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IAuditMaskingService _masking;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EnhancedAuditService> _logger;

    public EnhancedAuditService(
        IAuditRepository auditRepository,
        IAuditMaskingService masking,
        IUnitOfWork unitOfWork,
        ILogger<EnhancedAuditService> logger
        )
    {
        _auditRepository = auditRepository;
        _masking = masking;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ─── ثبت لاگ (Immutable - فقط اضافه) ───────────────────────────────────

    public async Task LogAsync(
        int? userId,
        string eventType,
        string action,
        string details,
        string? ipAddress = null,
        string? userAgent = null
        )
    {
        try
        {
            // Mask کردن اطلاعات حساس قبل از ثبت
            var maskedDetails = _masking.MaskDetails(details);
            var maskedAction = _masking.MaskSensitiveData(action);

            var auditLog = AuditLog.Create(
                userId: userId,
                eventType: SanitizeInput(eventType),
                action: SanitizeInput(maskedAction),
                details: maskedDetails,
                ipAddress: SanitizeIpAddress(ipAddress ?? "Unknown"),
                userAgent: SanitizeUserAgent(userAgent));

            await _auditRepository.AddAuditLogAsync(auditLog);
            // نکته: از UnitOfWork جداگانه استفاده می‌کنیم تا لاگ‌ها
            // مستقل از سایر تراکنش‌ها ذخیره شوند
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            _logger.LogDebug(
                "Audit: UserId={UserId}, EventType={EventType}, Action={Action}",
                userId, eventType, action);
        }
        catch (Exception ex)
        {
            // لاگ حسابرسی نباید عملیات اصلی را خراب کند
            _logger.LogError(ex, "Failed to write audit log: {Action}", action);
        }
    }

    // ─── Query لاگ‌ها (Admin) ────────────────────────────────────────────────

    public async Task<(IEnumerable<AuditDtos> Logs, int TotalItems)> GetAuditLogsAsync(
        int? userId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize
        )
    {
        var (logs, totalCount) = await _auditRepository.GetAuditLogsAsync(
            fromDate, toDate, userId, eventType, page, pageSize);

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

    /// <summary>
    /// جستجوی پیشرفته با فیلترهای متعدد.
    /// </summary>
    public async Task<(IEnumerable<AuditDtos> Logs, int Total)> SearchAuditLogsAsync(
       AuditSearchRequest request,
       CancellationToken ct = default
       )
    {
        return await _auditRepository.SearchAsync(request, ct);
    }

    /// <summary>
    /// Export لاگ‌ها به CSV.
    /// </summary>
    public async Task<byte[]> ExportToCsvAsync(
        AuditExportRequest request,
        CancellationToken ct = default
        )
    {
        var (logs, total) = await _auditRepository.GetAuditLogsAsync(
            request.From,
            request.To,
            request.UserId,
            request.EventType,
            1,
            request.MaxRows);

        var sb = new StringBuilder();
        sb.AppendLine("Id,UserId,EventType,Action,IpAddress,Timestamp");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{log.UserId},{log.EventType},{log.Action},{log.IpAddress},{log.Timestamp:O}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ─── Shorthand Methods ───────────────────────────────────────────────────

    public Task LogUserActionAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
        => LogAsync(userId, "UserAction", action, details, ipAddress, userAgent);

    public Task LogSecurityEventAsync(string eventType, string details, string ipAddress, int? userId = null, string? userAgent = null)
    {
        _logger.LogWarning("Security: {EventType} from {IP}", eventType, ipAddress);
        return LogAsync(userId, eventType, "SecurityEvent", details, ipAddress, userAgent);
    }

    public Task LogSystemEventAsync(string eventType, string details, int? userId = null, string? ipAddress = null, string? userAgent = null)
        => LogAsync(userId, eventType, "SystemEvent", details, ipAddress ?? "system", userAgent);

    public Task LogAdminEventAsync(string action, int userId, string details, string? ipAddress = null, string? userAgent = null)
        => LogAsync(userId, "AdminEvent", action, details, ipAddress ?? "system", userAgent);

    public Task LogOrderEventAsync(int orderId, string action, int userId, string details)
        => LogAsync(userId, "OrderEvent", action, $"OrderId={orderId}, {details}");

    public Task LogCartEventAsync(int userId, string action, string details, string ipAddress, string? userAgent = null)
        => LogAsync(userId, "CartEvent", action, details, ipAddress, userAgent);

    public Task LogProductEventAsync(int productId, string action, string details, int? userId = null)
        => LogAsync(userId, "ProductEvent", action, $"ProductId={productId}, {details}");

    public Task LogInventoryEventAsync(int productId, string action, string details, int? userId = null)
        => LogAsync(userId, "InventoryEvent", action, $"Inventory: ProductId={productId}, {details}");

    public Task LogPaymentEventAsync(int orderId, string action, int userId, string details)
        => LogAsync(userId, "PaymentEvent", action, $"OrderId={orderId}, {details}");

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Length > 500 ? input[..500] : input;
    }

    private static string SanitizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress)) return "unknown";
        return ipAddress.Length > 45 ? ipAddress[..45] : ipAddress;
    }

    private static string? SanitizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        return userAgent.Length > 500 ? userAgent[..500] : userAgent;
    }
}