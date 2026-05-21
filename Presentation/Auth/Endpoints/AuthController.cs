using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Application.Brand.Features.Shared;
using Presentation.Auth.Requests;

namespace Presentation.Auth.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (authenticateResult.Succeeded is false)
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

    [HttpPost("otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OtpRateLimit]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
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

    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OtpRateLimit]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
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

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
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

    [HttpDelete("session")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var command = new LogoutCommand(CurrentUser.UserId, request.RefreshToken);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var command = new LogoutAllCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}