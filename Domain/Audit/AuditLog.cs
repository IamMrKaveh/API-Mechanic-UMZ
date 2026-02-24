namespace Domain.Audit;

/// <summary>
/// لاگ حسابرسی Immutable - هیچ‌گاه ویرایش یا حذف نمی‌شود.
///
/// اصول:
/// - Immutability: پس از ایجاد هیچ فیلدی قابل تغییر نیست (به جز IsArchived)
/// - Append-Only: فقط رکوردهای جدید اضافه می‌شود
/// - Integrity Hash: برای تشخیص دستکاری
/// - Retention: سیاست نگه‌داری بر اساس نوع رویداد
/// </summary>
public sealed class AuditLog : BaseEntity
{
    public int? UserId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Details { get; private set; } = null!;
    public string IpAddress { get; private set; } = null!;
    public string? UserAgent { get; private set; }
    public DateTime Timestamp { get; private set; }

    
    /// <summary>Hash برای تشخیص دستکاری</summary>
    public string IntegrityHash { get; private set; } = null!;

    
    public bool IsArchived { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    private AuditLog()
    { }

    

    public static AuditLog Create(
        int? userId,
        string eventType,
        string action,
        string details,
        string ipAddress,
        string? userAgent = null)
    {
        Guard.Against.NullOrWhiteSpace(eventType, nameof(eventType));
        Guard.Against.NullOrWhiteSpace(action, nameof(action));

        var log = new AuditLog
        {
            UserId = userId,
            EventType = eventType,
            Action = action,
            Details = details ?? string.Empty,
            IpAddress = ipAddress ?? "unknown",
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
        };

        log.IntegrityHash = log.ComputeHash();
        return log;
    }

    

    /// <summary>
    /// علامت‌گذاری به عنوان Archived (برای لاگ‌های مالی که باید در DB بمانند).
    /// </summary>
    public void MarkAsArchived()
    {
        if (IsArchived) return;
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }

    

    /// <summary>
    /// بررسی Integrity لاگ (آیا دستکاری شده؟).
    /// </summary>
    public bool VerifyIntegrity()
    {
        return IntegrityHash == ComputeHash();
    }

    private string ComputeHash()
    {
        var data = $"{UserId}|{EventType}|{Action}|{Details}|{IpAddress}|{Timestamp:O}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    

    public bool IsFinancialEvent => EventType is
        "PaymentEvent" or "OrderEvent" or "RefundEvent" or "FinancialEvent";

    public bool IsSecurityEvent => EventType is
        "SecurityEvent" or "AuthEvent" or "AdminEvent" or "LoginEvent";
}