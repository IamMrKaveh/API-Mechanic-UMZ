using Application.Notification.Features.Commands.DeleteNotification;
using Application.Notification.Features.Commands.MarkAllNotificationsRead;
using Application.Notification.Features.Commands.MarkNotificationRead;
using Application.Notification.Features.Queries.GetNotifications;
using Application.Notification.Features.Queries.GetUnreadNotificationCount;
using MapsterMapper;
using Presentation.Notification.Requests;

namespace Presentation.Notification.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
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
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUnreadNotificationCountQuery(CurrentUser.UserId), ct);
        if (result.IsSuccess)
            return Ok(new { count = result.Value });
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var command = new MarkNotificationReadCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var command = new MarkAllNotificationsReadCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        var command = new DeleteNotificationCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}