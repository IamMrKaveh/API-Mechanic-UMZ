namespace MainApi.Auth.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// درخواست ارسال کد OTP به شماره موبایل
    /// </summary>
    [HttpPost("request-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RequestOtp([FromBody] LoginRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);

        var command = new RequestOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            IpAddress = clientIp
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(new { message = "کد تأیید ارسال شد." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// تأیید کد OTP و دریافت توکن دسترسی
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new VerifyOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            Code = request.Code,
            IpAddress = clientIp,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(result.Value);

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// تمدید توکن دسترسی با استفاده از Refresh Token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            IpAddress = clientIp,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(result.Value);

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// خروج از سیستم (ابطال نشست فعلی)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        if (_currentUserService.CurrentUser.UserId < 1)
            return Unauthorized();

        var command = new LogoutCommand
        {
            UserId = _currentUserService.CurrentUser.UserId,
            RefreshToken = request.RefreshToken
        };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(new { message = "با موفقیت خارج شدید." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// خروج از تمام دستگاه‌ها (ابطال تمام نشست‌ها)
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        if (_currentUserService.CurrentUser.UserId < 1)
            return Unauthorized();

        var command = new LogoutAllCommand(_currentUserService.CurrentUser.UserId);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
            return Ok(new { message = "از تمام دستگاه‌ها خارج شدید." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }
}