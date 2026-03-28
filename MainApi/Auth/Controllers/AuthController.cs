using MainApi.Auth.Requests;

namespace MainApi.Auth.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// درخواست ارسال کد OTP به شماره موبایل
    /// </summary>
    [HttpPost("request-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RequestOtp([FromBody] LoginRequest request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);

        var command = new RequestOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            IpAddress = clientIp
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
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
        return ToActionResult(result);
    }

    /// <summary>
    /// تمدید توکن دسترسی با استفاده از Refresh Token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
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
        return ToActionResult(result);
    }

    /// <summary>
    /// خروج از سیستم (ابطال نشست فعلی)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var command = new LogoutCommand
        {
            UserId = CurrentUser.UserId,
            RefreshToken = request.RefreshToken
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// خروج از تمام دستگاه‌ها (ابطال تمام نشست‌ها)
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}