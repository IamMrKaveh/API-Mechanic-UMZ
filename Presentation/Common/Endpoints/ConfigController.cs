using Application.Auth.Features.Shared;

namespace Presentation.Common.Endpoints;

public sealed record AuthPublicConfigDto(
    int OtpLength,
    int OtpResendSeconds,
    int OtpExpirationMinutes,
    int SessionExpirationDays);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/config")]
[AllowAnonymous]
public sealed class ConfigController(
    IOptions<AuthOptions> authOptions,
    IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthPublicConfigDto>), StatusCodes.Status200OK)]
    public IActionResult GetAuthConfig()
    {
        // Single source of truth: the Auth section. The public config must report
        // exactly the values the OTP pipeline actually uses (see SendOtpHandler).
        var auth = authOptions.Value;

        var dto = new AuthPublicConfigDto(
            OtpLength: auth.OtpLength,
            OtpResendSeconds: auth.OtpExpirationMinutes * 60,
            OtpExpirationMinutes: auth.OtpExpirationMinutes,
            SessionExpirationDays: auth.SessionExpirationDays);

        return Ok(new ApiResponse<AuthPublicConfigDto>(dto, true, null));
    }
}