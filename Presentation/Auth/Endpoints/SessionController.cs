using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetUserSessions;

namespace Presentation.Auth.Endpoints;

[Route("api/sessions")]
[ApiController]
[Authorize]
public class SessionController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetActiveSessions()
    {
        var query = new GetUserSessionsQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId)
    {
        var command = new RevokeSessionCommand(CurrentUser.UserId, sessionId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}