using Application.Auth.Features.Commands.AdminRevokeSession;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Queries.GetUserSessions;
using Application.Auth.Features.Shared;

namespace Presentation.Session.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users/{userId:guid}/sessions")]
[Authorize(Roles = "Admin")]
public sealed class AdminSessionController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSessions(Guid userId, CancellationToken ct)
    {
        var query = new GetUserSessionsQuery(userId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeUserSession(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var command = new AdminRevokeSessionCommand(userId, sessionId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllUserSessions(Guid userId, CancellationToken ct)
    {
        var command = new LogoutAllCommand(userId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}