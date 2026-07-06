using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetUserSessions;
using Application.Auth.Features.Shared;

namespace Presentation.Session.Endpoints;

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
        var query = new GetUserSessionsQuery(
            RequestContext.UserId ?? Guid.Empty,
            RequestContext.SessionId);
        return await Send(query, ct);
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<CurrentSessionDto>), StatusCodes.Status200OK)]
    public IActionResult GetCurrentSession()
    {
        var sessionId = RequestContext.SessionId;
        var payload = new CurrentSessionDto { SessionId = sessionId };
        return Ok(new ApiResponse<CurrentSessionDto>(payload, true, null));
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        return await Send(new LogoutAllCommand(RequestContext.UserId!.Value), ct);
    }

    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        return await Send(new RevokeSessionCommand(RequestContext.UserId!.Value, sessionId), ct);
    }
}

public sealed record CurrentSessionDto
{
    public Guid? SessionId { get; init; }
}