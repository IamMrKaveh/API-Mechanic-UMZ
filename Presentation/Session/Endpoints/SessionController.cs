using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.LogoutOthers;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetCurrentSession;
using Application.Auth.Features.Queries.GetUserSessions;
using Application.Auth.Features.Shared;

namespace Presentation.Session.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/sessions")]
[Authorize]
public sealed class SessionController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserSessionDto>>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetActiveSessions(CancellationToken ct)
        => Send(new GetUserSessionsQuery(), ct);

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<CurrentSessionDto>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetCurrentSession(CancellationToken ct)
        => Send(new GetCurrentSessionQuery(), ct);

    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
        => Send(new RevokeSessionCommand(sessionId), ct);

    [HttpDelete("others")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> LogoutOtherSessions(CancellationToken ct)
        => Send(new LogoutOthersCommand(), ct);

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> LogoutAllSessions(CancellationToken ct)
        => Send(new LogoutAllCommand(), ct);
}
