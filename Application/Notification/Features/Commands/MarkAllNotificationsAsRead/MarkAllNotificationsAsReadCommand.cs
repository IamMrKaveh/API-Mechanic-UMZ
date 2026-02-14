namespace Application.Notification.Features.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(int UserId) : IRequest<ServiceResult<int>>;