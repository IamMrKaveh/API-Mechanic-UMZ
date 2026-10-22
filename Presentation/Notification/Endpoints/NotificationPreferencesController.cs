using Application.Notification.Features.Commands.UpdateNotificationPreferences;
using Application.Notification.Features.Queries.GetNotificationPreferences;
using Application.Notification.Features.Shared;
using Presentation.Notification.Requests;

namespace Presentation.Notification.Endpoints;

/// <summary>
/// The notification preferences of the signed-in user: which notification types
/// the user does not want to receive.
/// </summary>
[Route("api/v{version:apiVersion}/notification-preferences")]
[ApiController]
[Authorize]
public class NotificationPreferencesController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetNotificationPreferencesQuery(), ct);
        return ToActionResult(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken ct)
    {
        var command = new UpdateNotificationPreferencesCommand(request.DisabledTypeNames);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}
