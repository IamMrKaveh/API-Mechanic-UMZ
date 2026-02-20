using Application.Review.Features.Shared;

namespace Application.User.Contracts;

/// <summary>
/// سرویس کوئری کاربران - خواندن مستقیم DTO
/// پیاده‌سازی در Infrastructure
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// دریافت پروفایل کاربر
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده کاربران (Admin)
    /// </summary>
    Task<PaginatedResult<UserProfileDto>> GetUsersPagedAsync(
        string? search,
        bool? isActive,
        bool? isAdmin,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت آدرس‌های کاربر
    /// </summary>
    Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت نشست‌های فعال کاربر
    /// </summary>
    Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت داشبورد کاربر
    /// </summary>
    Task<UserDashboardDto?> GetUserDashboardAsync(int userId, CancellationToken ct = default);

    Task<PaginatedResult<ProductReviewDto>> GetUserReviewsPagedAsync(
    int userId,
    int page,
    int pageSize,
    CancellationToken ct = default);

    Task<PaginatedResult<WishlistItemDto>> GetUserWishlistPagedAsync(
    int userId,
    int page,
    int pageSize,
    CancellationToken ct = default);
}

/// <summary>
/// DTO نشست کاربر
/// </summary>
public class UserSessionDto
{
    public int Id { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string CreatedByIp { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? BrowserInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrent { get; set; }
}