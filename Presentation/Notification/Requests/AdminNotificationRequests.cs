namespace Presentation.Notification.Requests;

public record AdminSendNotificationRequest(
    string Title,
    string Message,
    string Type,
    string? ActionUrl,
    bool? SendToAll,
    Guid? UserId);