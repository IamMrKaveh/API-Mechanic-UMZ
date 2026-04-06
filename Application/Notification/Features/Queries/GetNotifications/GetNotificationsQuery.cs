using Application.Common.Results;
using Application.Notification.Features.Shared;
using SharedKernel.Models;

namespace Application.Notification.Features.Queries.GetNotifications;

public record GetNotificationsQuery(
    int UserId,
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<NotificationDto>>>;