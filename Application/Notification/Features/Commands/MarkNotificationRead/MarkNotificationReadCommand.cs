using Application.Common.Results;

namespace Application.Notification.Features.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<ServiceResult>;