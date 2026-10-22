namespace Application.Notification.Features.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    IReadOnlyCollection<string> DisabledTypeNames) : ICommand<Unit>, IAuditableCommand
{
    public string AuditEventType => "Notification";

    public string AuditAction => "UpdateNotificationPreferences";

    public string? AuditEntityType => "User";

    public string? AuditEntityId => null;
}
