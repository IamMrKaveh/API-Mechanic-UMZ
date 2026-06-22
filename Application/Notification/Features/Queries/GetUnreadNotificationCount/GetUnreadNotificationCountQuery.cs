namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery()
    : IQuery<int>;