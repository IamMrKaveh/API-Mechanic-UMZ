namespace Application.Notification.Features.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(
    int NotificationId,
    int UserId) : IRequest<ServiceResult<bool>>;