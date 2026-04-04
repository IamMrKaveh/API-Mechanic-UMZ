using SharedKernel.Models;

namespace SharedKernel.Contracts;

/// <summary>
/// سرویس دسترسی به اطلاعات کاربر جاری
/// </summary>
public interface ICurrentUserService
{
    CurrentUser CurrentUser { get; }
    bool IsAuthenticated { get; }
    string? UserAgent { get; }
    string? GuestId { get; }
}