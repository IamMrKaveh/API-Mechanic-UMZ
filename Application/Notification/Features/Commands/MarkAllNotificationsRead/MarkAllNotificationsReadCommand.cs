namespace Application.Notification.Features.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<ServiceResult>;