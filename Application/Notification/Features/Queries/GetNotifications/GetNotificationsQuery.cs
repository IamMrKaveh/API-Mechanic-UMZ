using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetNotifications;

public record GetNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false) : IRequest<ServiceResult<PaginatedResult<NotificationDto>>>;