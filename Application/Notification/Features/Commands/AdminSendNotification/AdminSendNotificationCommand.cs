namespace Application.Notification.Features.Commands.AdminSendNotification;

public record AdminSendNotificationCommand(
	string Title,
	string Message,
	string Type,
	string? ActionUrl,
	bool SendToAll,
	Guid? UserId)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "Notification";

	public string AuditAction => "AdminSendNotification";

	public string? AuditEntityType => "Notification";

	public string? AuditEntityId => SendToAll ? "ALL" : UserId?.ToString();
}