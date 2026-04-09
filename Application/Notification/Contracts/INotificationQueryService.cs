using Application.Notification.Features.Shared;

namespace Application.Notification.Contracts;

public interface INotificationQueryService
{
    Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}