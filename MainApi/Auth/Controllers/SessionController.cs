namespace MainApi.Auth.Controllers;

[Route("api/sessions")]
[ApiController]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public SessionController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// دریافت لیست نشست‌های فعال کاربر جاری
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetActiveSessions()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var query = new GetUserSessionsQuery(_currentUserService.UserId.Value);
        var result = await _mediator.Send(query);

        if (result.IsSucceed)
            return Ok(result.Data);

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// ابطال یک نشست خاص
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> RevokeSession(int sessionId)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var command = new RevokeSessionCommand(_currentUserService.UserId.Value, sessionId);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
            return Ok(new { message = "نشست با موفقیت ابطال شد." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// ابطال تمام نشست‌ها به جز نشست فعلی
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> RevokeAllSessions()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var command = new LogoutAllCommand(_currentUserService.UserId.Value);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
            return Ok(new { message = "تمام نشست‌ها ابطال شدند." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }
}