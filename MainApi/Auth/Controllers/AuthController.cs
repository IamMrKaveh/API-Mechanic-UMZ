namespace MainApi.Auth.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// درخواست ارسال کد OTP به شماره موبا��ل
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

        if (result.IsSucceed)
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

        if (result.IsSucceed)
            return Ok(result.Data);

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
            RefreshToken = request.refreshToken,
            IpAddress = clientIp,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(command);

        if (result.IsSucceed)
            return Ok(result.Data);

        return StatusCode(result.StatusCode, new { message = result.Error });
    }

    /// <summary>
    /// خروج از سیستم (ابطال نشست فعلی)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var command = new LogoutCommand
        {
            UserId = _currentUserService.UserId.Value,
            RefreshToken = request.refreshToken
        };

        var result = await _mediator.Send(command);

        if (result.IsSucceed)
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
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var command = new LogoutAllCommand(_currentUserService.UserId.Value);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
            return Ok(new { message = "از تمام دستگاه‌ها خارج شدید." });

        return StatusCode(result.StatusCode, new { message = result.Error });
    }
}