using Application.Common.Results;

namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(int UserId) : IRequest<ServiceResult<int>>;