namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery() : IRequest<ServiceResult<int>>;