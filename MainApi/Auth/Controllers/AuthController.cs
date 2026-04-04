using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.RequestOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using MainApi.Auth.Requests;
using MainApi.Extensions;

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
    public async Task<IActionResult> RequestOtp(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var command = new RequestOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            IpAddress = HttpContextHelper.GetClientIpAddress(HttpContext)
        };

        var result = await _mediator.Send(command, ct);
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
        var command = new VerifyOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            Code = request.Code,
            IpAddress = HttpContextHelper.GetClientIpAddress(HttpContext),
            UserAgent = Request.Headers.UserAgent.ToString()
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
        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            IpAddress = HttpContextHelper.GetClientIpAddress(HttpContext),
            UserAgent = Request.Headers.UserAgent.ToString()
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