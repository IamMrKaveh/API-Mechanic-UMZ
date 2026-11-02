using Application.Auth.Contracts;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Infrastructure.Common.Options;
using Infrastructure.Security.Settings;
using Presentation.Auth;
using Presentation.Auth.Requests;
using Presentation.Auth.Support;
using Presentation.Common.Cookies;

namespace Presentation.Auth.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IMediator mediator,
    IMapper mapper,
    IGoogleAuthenticationService googleAuthService,
    IAuthCookieService cookieService,
    IOptions<FrontendUrlsOptions> frontendOptions,
    IOptions<SecuritySettings> securityOptions)
    : BaseApiController(mediator, mapper)
{
    private readonly FrontendUrlsOptions _frontendOptions = frontendOptions.Value;
    private readonly List<string> _allowedOrigins = securityOptions.Value.AllowedOrigins;

    [HttpGet("google")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl)
    {
        // The frontend wants the browser back on its own origin after the
        // round trip; the callback receives this as a query parameter.
        var safeReturnUrl = ReturnUrlPolicy.Sanitize(returnUrl);
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl = safeReturnUrl })
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl,
        CancellationToken ct)
    {
        var safeReturnUrl = ReturnUrlPolicy.Sanitize(returnUrl);
        var profile = await googleAuthService.AuthenticateAsync(ct);

        if (profile is null)
            return Redirect(BuildFrontendUrl(_frontendOptions.GoogleAuthFailurePath));

        var command = new GoogleLoginCommand(
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.ProviderKey);

        var result = await Mediator.Send(command, ct);

        if (!result.IsSuccess)
            return Redirect(BuildFrontendUrl(_frontendOptions.GoogleAuthFailurePath));

        cookieService.WriteRefreshToken(Response, result.Value.RefreshToken);

        // The external cookie has served its purpose; dropping it here keeps
        // the browser from carrying a stale principal into a later attempt.
        await HttpContext.SignOutAsync(AuthSchemes.External);

        var completePath = _frontendOptions.GoogleAuthCompletePath;
        var separator = completePath.Contains('?') ? '&' : '?';

        return Redirect(
            BuildFrontendUrl(
                $"{completePath}{separator}returnUrl={Uri.EscapeDataString(safeReturnUrl)}"));
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

    /// <summary>
    /// Resolves the frontend origin for a browser redirect.
    /// </summary>
    /// <remarks>
    /// Only an allow-listed Origin header is honored; otherwise the configured
    /// frontend base URL is used. Accepting an arbitrary Origin here would turn
    /// the callback into an open redirect.
    /// </remarks>
    private string BuildFrontendUrl(string path)
        => ResolveFrontendBaseUrl().TrimEnd('/') + path;

    private string ResolveFrontendBaseUrl()
    {
        var origin = Request.Headers["Origin"].ToString();
        if (!string.IsNullOrWhiteSpace(origin) &&
            Uri.IsWellFormedUriString(origin, UriKind.Absolute) &&
            _allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            return origin;
        }

        if (!string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl))
            return _frontendOptions.BaseUrl;

        return _frontendOptions.LocalHostUrl;
    }
}
