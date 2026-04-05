using System.Security.Claims;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.RequestOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using MainApi.Auth.Requests;
using MainApi.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace MainApi.Auth.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

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

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleCallback") };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!authenticateResult.Succeeded)
            return BadRequest("Google authentication failed.");

        var claims = authenticateResult.Principal.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var firstName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var lastName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
            return BadRequest("Incomplete profile data from Google.");

        var command = new GoogleLoginCommand(email, firstName ?? "", lastName ?? "", providerKey);
        var result = await _mediator.Send(command, ct);

        return ToActionResult(result);
    }
}