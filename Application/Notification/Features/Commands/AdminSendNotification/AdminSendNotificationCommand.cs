namespace Application.Notification.Features.Commands.AdminSendNotification;

public record AdminSendNotificationCommand(
    string Title,
    string Message,
    string Type,
    string? ActionUrl,
    bool SendToAll,
    Guid? UserId) : IRequest<ServiceResult>;