using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Presentation.Auth.Requests;
using Presentation.Common.Extensions;
using Presentation.Common.Filters;
using System.Security.Claims;

namespace Presentation.Auth.Endpoints;

[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpPost("request-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OtpRateLimit]
    public async Task<IActionResult> RequestOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken ct)
    {
        var command = new SendOtpCommand(
            request.PhoneNumber,
            HttpContextHelper.GetClientIpAddress(HttpContext));

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OtpRateLimit]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken ct)
    {
        var command = new VerifyOtpCommand(
            request.PhoneNumber,
            request.Code,
            HttpContextHelper.GetClientIpAddress(HttpContext),
            Request.Headers.UserAgent.ToString());

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken,
            HttpContextHelper.GetClientIpAddress(HttpContext),
            Request.Headers.UserAgent.ToString());

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var command = new LogoutCommand(CurrentUser.UserId, request.RefreshToken);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await Mediator.Send(new LogoutAllCommand(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
            return BadRequest("Google authentication failed.");

        var claims = authenticateResult.Principal?.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var firstName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        var lastName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
            return BadRequest("Incomplete profile data from Google.");

        var command = new GoogleLoginCommand(email, firstName ?? string.Empty, lastName ?? string.Empty, providerKey);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}