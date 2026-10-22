namespace Presentation.Notification.Requests;

public record UpdateNotificationPreferencesRequest(
    IReadOnlyCollection<string> DisabledTypeNames);
