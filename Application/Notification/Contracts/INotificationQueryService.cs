using Application.Notification.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Notification.Contracts;

public interface INotificationQueryService
{
    Task<PaginatedResult<NotificationDto>> GetByUserIdAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken ct = default);
}