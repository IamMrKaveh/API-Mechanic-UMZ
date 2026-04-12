using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetUserSessions;
using MapsterMapper;

namespace Presentation.Auth.Endpoints;

[Route("api/sessions")]
[ApiController]
[Authorize]
public class SessionController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        var query = new GetUserSessionsQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        var command = new RevokeSessionCommand(CurrentUser.UserId, sessionId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}