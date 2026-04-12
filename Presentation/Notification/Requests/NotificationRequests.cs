namespace Presentation.Notification.Requests;

public record GetNotificationsRequest(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20);