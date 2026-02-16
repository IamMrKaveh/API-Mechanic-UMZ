namespace Application.Notification.Features.Commands.DeleteNotification;

public record DeleteNotificationCommand(int NotificationId, int UserId) : IRequest<ServiceResult>;