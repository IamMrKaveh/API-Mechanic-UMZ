namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(Guid UserId) : IRequest<ServiceResult<int>>;