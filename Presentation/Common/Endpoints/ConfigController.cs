using Application.Auth.Features.Shared;
using Infrastructure.Auth.Options;

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
    IOptions<OtpOptions> otpOptions,
    IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthPublicConfigDto>), StatusCodes.Status200OK)]
    public IActionResult GetAuthConfig()
    {
        var otp = otpOptions.Value;
        var auth = authOptions.Value;

        var dto = new AuthPublicConfigDto(
            OtpLength: otp.Length,
            OtpResendSeconds: otp.ExpirationMinutes * 60,
            OtpExpirationMinutes: otp.ExpirationMinutes,
            SessionExpirationDays: auth.SessionExpirationDays);

        return Ok(new ApiResponse<AuthPublicConfigDto>(dto, true, null));
    }
}