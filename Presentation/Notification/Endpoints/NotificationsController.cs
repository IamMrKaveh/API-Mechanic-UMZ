using Application.Notification.Features.Commands.DeleteNotification;
using Application.Notification.Features.Commands.MarkAllNotificationsRead;
using Application.Notification.Features.Commands.MarkNotificationRead;
using Application.Notification.Features.Queries.GetNotifications;
using Application.Notification.Features.Queries.GetUnreadNotificationCount;

namespace Presentation.Notification.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetNotificationsQuery(
            CurrentUser.UserId,
            unreadOnly,
            page,
            pageSize);

        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadNotificationCountQuery(CurrentUser.UserId));
        if (result.IsSuccess) return Ok(new { count = result.Value });
        return ToActionResult(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var command = new MarkNotificationReadCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var command = new MarkAllNotificationsReadCommand(CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var command = new DeleteNotificationCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}