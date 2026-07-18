using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Queries.GetCurrentSession;
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
        var query = new GetUserSessionsQuery();
        return await Send(query, ct);
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<CurrentSessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentSession(CancellationToken ct)
        => await Send(new GetCurrentSessionQuery(), ct);

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
        => await Send(new LogoutAllCommand(), ct);
}