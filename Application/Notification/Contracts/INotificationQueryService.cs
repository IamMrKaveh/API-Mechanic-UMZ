using Application.Common.Models;

namespace Application.Notification.Contracts;

public interface INotificationQueryService
{
    Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(
        int userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IEnumerable<NotificationDto>> GetUnreadByUserIdAsync(
        int userId,
        int? limit = null,
        CancellationToken ct = default);

    Task<int> CountUnreadByUserIdAsync(int userId, CancellationToken ct = default);

    Task<IEnumerable<NotificationDto>> GetByRelatedEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<IEnumerable<NotificationDto>> GetRecentByUserIdAsync(
        int userId,
        int count,
        CancellationToken ct = default);
}