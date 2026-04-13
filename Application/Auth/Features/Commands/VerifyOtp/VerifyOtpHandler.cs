using Application.Auth.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IOtpService otpService,
    IAuthService authService,
    IAuditService auditService) : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var ipAddress = IpAddress.Create(request.IpAddress);
        var otpCode = OtpCode.Create(request.Code);

        var verifyResult = await otpService.VerifyOtpAsync(phoneNumber, otpCode, request.Purpose, ct);
        if (!verifyResult.IsSuccess)
            return ServiceResult<AuthResult>.Failure(verifyResult.Error.Message);

        var authResult = await authService.VerifyOtpAsync(phoneNumber, otpCode, ipAddress, request.UserAgent, ct);
        if (!authResult.IsSuccess)
            return ServiceResult<AuthResult>.Failure(authResult.Error);

        await auditService.LogSecurityEventAsync(
            "VerifyOtp",
            $"OTP برای شماره {request.PhoneNumber} تأیید شد.",
            ipAddress,
            ct);

        var (accessToken, refreshToken, user, _) = authResult.Value;
        return ServiceResult<AuthResult>.Success(new AuthResult(accessToken, refreshToken, user));
    }
}