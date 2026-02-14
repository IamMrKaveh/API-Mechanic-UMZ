namespace MainApi.Notification.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserNotificationsQuery(CurrentUser.UserId.Value, unreadOnly ? false : null, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUnreadNotificationCountQuery(CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        if (result.IsSucceed) return Ok(new { count = result.Data });
        return ToActionResult(result);
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new MarkNotificationAsReadCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new MarkAllNotificationsAsReadCommand(CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    // DeleteNotificationCommand needs to be implemented
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        // var command = new DeleteNotificationCommand(id, CurrentUser.UserId.Value);
        // var result = await _mediator.Send(command);
        // return ToActionResult(result);
        return StatusCode(501, "Implement DeleteNotificationCommand");
    }
}