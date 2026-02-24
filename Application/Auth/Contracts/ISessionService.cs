namespace Application.Auth.Contracts;

/// <summary>
/// مدیریت نشست‌های کاربر
/// پیاده‌سازی در Infrastructure (Database + اختیاری Redis)
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// ایجاد نشست جدید برای کاربر
    /// </summary>
    Task<UserSessionInfo> CreateSessionAsync(
        int userId,
        string tokenSelector,
        string tokenVerifierHash,
        string ipAddress,
        string? userAgent,
        string sessionType = "Web",
        int expiryDays = 30,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت نشست با TokenSelector
    /// </summary>
    Task<UserSessionInfo?> GetSessionBySelectorAsync(
        string tokenSelector,
        CancellationToken ct = default
        );

    /// <summary>
    /// اعتبارسنجی نشست
    /// </summary>
    Task<bool> ValidateSessionAsync(
        string tokenSelector,
        string tokenVerifierHash,
        CancellationToken ct = default
        );

    /// <summary>
    /// ابطال یک نشست
    /// </summary>
    Task RevokeSessionAsync(
        int sessionId,
        CancellationToken ct = default
        );

    /// <summary>
    /// ابطال تمام نشست‌های کاربر
    /// </summary>
    Task RevokeAllUserSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت نشست‌های فعال کاربر
    /// </summary>
    Task<IEnumerable<UserSessionInfo>> GetActiveSessionsAsync(
        int userId,
        CancellationToken ct = default
        );

    /// <summary>
    /// پاکسازی نشست‌های منقضی
    /// </summary>
    Task CleanupExpiredSessionsAsync(
        CancellationToken ct = default
        );
}

/// <summary>
/// اطلاعات نشست (DTO داخلی Application)
/// </summary>
public class UserSessionInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenSelector { get; set; } = null!;
    public string TokenVerifierHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string CreatedByIp { get; set; } = null!;
    public string? UserAgent { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string SessionType { get; set; } = "Web";
    public DateTime? LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}