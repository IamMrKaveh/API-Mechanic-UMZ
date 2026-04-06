using Presentation.Base.Controllers.v1;
using SharedKernel.Contracts;

namespace Presentation.Auth.Controllers;

[Route("api/sessions")]
[ApiController]
[Authorize]
public class SessionController(IMediator mediator, ICurrentUserService currentUserService) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// دریافت لیست نشست‌های فعال کاربر جاری
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveSessions()
    {
        var query = new GetUserSessionsQuery(_currentUserService.CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    /// <summary>
    /// ابطال یک نشست خاص
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> RevokeSession(int sessionId)
    {
        var command = new RevokeSessionCommand(_currentUserService.CurrentUser.UserId, sessionId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// ابطال تمام نشست‌ها به جز نشست فعلی
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var command = new LogoutAllCommand(_currentUserService.CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}