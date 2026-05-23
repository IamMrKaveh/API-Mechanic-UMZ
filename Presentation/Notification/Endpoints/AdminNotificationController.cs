using Application.Notification.Features.Commands.AdminDeleteNotification;
using Application.Notification.Features.Commands.AdminSendNotification;
using Application.Notification.Features.Queries.GetAllNotifications;
using Application.Notification.Features.Shared;
using Presentation.Notification.Requests;

namespace Presentation.Notification.Endpoints;

[Route("api/v{version:apiVersion}/admin/notifications")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminNotificationController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAllNotificationsQuery(page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(
        [FromBody] AdminSendNotificationRequest request,
        CancellationToken ct)
    {
        var command = new AdminSendNotificationCommand(
            request.Title,
            request.Message,
            request.Type,
            request.ActionUrl,
            request.SendToAll ?? false,
            request.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new AdminDeleteNotificationCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}