namespace Application.Notification.Features.Commands.AdminDeleteNotification;

public record AdminDeleteNotificationCommand(
	Guid NotificationId)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "Notification";

	public string AuditAction => "AdminDeleteNotification";

	public string? AuditEntityType => "Notification";

	public string? AuditEntityId => NotificationId.ToString();
}