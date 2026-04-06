using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Common.Results;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IAuthService authService,
    ILogger<VerifyOtpHandler> logger) : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<VerifyOtpHandler> _logger = logger;

    public async Task<ServiceResult<AuthResult>> Handle(
        VerifyOtpCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation("OTP verify for {Phone}", request.PhoneNumber);

        var result = await _authService.VerifyOtpAsync(
            request.PhoneNumber,
            request.Code,
            request.IpAddress,
            request.UserAgent,
            ct);

        if (result.IsFailure)
            return ServiceResult<AuthResult>.Failure(result.Error ?? "");

        var (accessToken, refreshToken, user, isNewUser) = result.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.FullToken,
            User = user,
            IsNewUser = isNewUser
        });
    }
}