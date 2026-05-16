using Application.Notification.Features.Commands.DeleteNotification;
using Application.Notification.Features.Commands.MarkAllNotificationsRead;
using Application.Notification.Features.Commands.MarkNotificationRead;
using Application.Notification.Features.Queries.GetNotifications;
using Application.Notification.Features.Queries.GetUnreadNotificationCount;
using Application.Notification.Features.Shared;
using Presentation.Notification.Requests;

namespace Presentation.Notification.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] GetNotificationsRequest request,
        CancellationToken ct)
    {
        var query = new GetNotificationsQuery(
            CurrentUser.UserId,
            request.UnreadOnly,
            request.Page,
            request.PageSize);

        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var query = new GetUnreadNotificationCountQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var command = new MarkNotificationReadCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var command = new MarkAllNotificationsReadCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        var command = new DeleteNotificationCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}