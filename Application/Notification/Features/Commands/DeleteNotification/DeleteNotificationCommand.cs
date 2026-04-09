namespace Application.Notification.Features.Commands.DeleteNotification;

public record DeleteNotificationCommand(Guid NotificationId, Guid UserId) : IRequest<ServiceResult>;