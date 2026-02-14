namespace Application.Security.Contracts;

/// <summary>
/// سرویس دسترسی به اطلاعات کاربر جاری
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? GuestId { get; }
    string? PhoneNumber { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}