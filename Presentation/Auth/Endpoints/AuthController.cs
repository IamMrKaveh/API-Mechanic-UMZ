using Application.Auth.Contracts;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Presentation.Auth.Requests;

namespace Presentation.Auth.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IMediator mediator,
    IMapper mapper,
    IGoogleAuthenticationService googleAuthService)
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
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleCallback(CancellationToken ct)
    {
        var profile = await googleAuthService.AuthenticateAsync(ct);

        if (profile is null)
            return BadRequest("Google authentication failed.");

        var command = new GoogleLoginCommand(
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.ProviderKey);

        return await Send(command, ct);
    }

    [HttpPost("otp")]
    [AllowAnonymous]
    [OtpRateLimit]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] SendOtpRequest request,
        CancellationToken ct)
    {
        var command = new SendOtpCommand(request.PhoneNumber);
        var result = await Mediator.Send(command, ct);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, new ApiResponse(true, null));

        return ToActionResult(result);
    }

    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [OtpRateLimit]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken ct)
    {
        var command = new VerifyOtpCommand(request.PhoneNumber, request.Code, request.DeviceInfo);
        var result = await Mediator.Send(command, ct);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthResult>(result.Value, true, null));

        return ToActionResult(result);
    }

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await Mediator.Send(command, ct);

        if (result.IsSuccess)
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthResult>(result.Value, true, null));

        return ToCreatedActionResult(result);
    }

    [HttpDelete("session")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        return await Send(new LogoutCommand(request.RefreshToken), ct);
    }

    [HttpDelete("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        return await Send(new LogoutAllCommand(), ct);
    }
}