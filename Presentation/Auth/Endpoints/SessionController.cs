using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetUserSessions;
using Application.User.Features.Shared;

namespace Presentation.Auth.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/sessions")]
[Authorize]
public class SessionController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        var query = new GetUserSessionsQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var command = new RevokeSessionCommand(CurrentUser.UserId, sessionId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}