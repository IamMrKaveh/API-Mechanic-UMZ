using Application.Auth.Contracts;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Presentation.Auth.Requests;
using Presentation.Common.Cookies;

namespace Presentation.Auth.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IMediator mediator,
    IMapper mapper,
    IGoogleAuthenticationService googleAuthService,
    IAuthCookieService cookieService)
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
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
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

        var result = await Mediator.Send(command, ct);

        return HandleAuthResult(result);
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
    [ProducesResponseType(typeof(ApiResponse<AuthResultResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken ct)
    {
        var command = new VerifyOtpCommand(request.PhoneNumber, request.Code, request.DeviceInfo);
        var result = await Mediator.Send(command, ct);

        return HandleAuthResult(result, StatusCodes.Status201Created);
    }

    [HttpPost("token/refresh")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse<AuthResultResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var refreshToken = cookieService.ReadRefreshToken(Request);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new ApiResponse(false, "توکن به‌روزرسانی یافت نشد."));

        var command = new RefreshTokenCommand(refreshToken);
        var result = await Mediator.Send(command, ct);

        return HandleAuthResult(result, StatusCodes.Status201Created);
    }

    [HttpDelete("session")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = cookieService.ReadRefreshToken(Request);
        var result = await Send(new LogoutCommand(refreshToken), ct);

        cookieService.ClearRefreshToken(Response);

        return result;
    }

    [HttpDelete("sessions")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await Send(new LogoutAllCommand(), ct);

        cookieService.ClearRefreshToken(Response);

        return result;
    }

    private IActionResult HandleAuthResult(ServiceResult<AuthResult> result, int statusCode = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess)
            return ToActionResult(result);

        var auth = result.Value;
        cookieService.WriteRefreshToken(Response, auth.RefreshToken, auth.RefreshTokenExpiresAt);

        return StatusCode(statusCode, new ApiResponse<AuthResultResponse>(AuthResultResponse.FromAuthResult(auth), true, null));
    }

    private IActionResult HandleAuthResult(ServiceResult<TokenResultDto> result)
    {
        if (!result.IsSuccess)
            return ToActionResult(result);

        cookieService.WriteRefreshToken(Response, result.Value.RefreshToken);

        return Ok(new ApiResponse(true, null));
    }
}
