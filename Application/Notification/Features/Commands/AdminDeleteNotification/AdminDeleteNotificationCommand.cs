namespace Application.Notification.Features.Commands.AdminDeleteNotification;

public record AdminDeleteNotificationCommand(
    Guid NotificationId)
    : ICommand;