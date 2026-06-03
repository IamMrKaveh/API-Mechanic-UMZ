using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    ISessionService sessionService,
    IJwtTokenGenerator jwtTokenGenerator,
    IAuditService auditService)
    : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(
        VerifyOtpCommand request,
        CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var ipAddress = IpAddress.Create(request.IpAddress);
        var otpCode = OtpCode.Create(request.Code);

        var user = await userRepository.GetByPhoneNumberAsync(
            phoneNumber,
            ct);

        if (user is null)
            return ServiceResult<AuthResult>.Failure(
                "کاربری با این شماره یافت نشد.");

        var otp = await otpRepository.GetLatestActiveByUserIdAsync(
            user.Id,
            request.Purpose,
            ct);

        if (otp is null)
            return ServiceResult<AuthResult>.Failure(
                "کد OTP فعالی یافت نشد.");

        try
        {
            otp.Verify(otpCode);
        }
        catch (DomainException ex)
        {
            otpRepository.Update(otp);

            return ServiceResult<AuthResult>.Failure(
                ex.Message);
        }

        otpRepository.Update(otp);

        var sessionResult = await sessionService.CreateSessionAsync(
            user.Id,
            ipAddress,
            request.UserAgent,
            ct);

        if (sessionResult.IsSuccess is false)
            return ServiceResult<AuthResult>.Failure(
                sessionResult.Error);

        await auditService.LogSecurityEventAsync(
            "VerifyOtp",
            $"OTP برای شماره {request.PhoneNumber} تأیید شد.",
            ipAddress,
            user.Id,
            ct);

        var jwtAccessToken =
            jwtTokenGenerator.GenerateAccessToken(user);

        var refreshSession = sessionResult.Value!;

        var userDto =
            user.Adapt<UserProfileDto>();

        return ServiceResult<AuthResult>.Success(
            new AuthResult
            {
                AccessToken = jwtAccessToken,

                RefreshToken = refreshSession.RefreshToken,

                AccessTokenExpiresAt =
                    DateTime.UtcNow.AddMinutes(60),

                RefreshTokenExpiresAt =
                    refreshSession.ExpiresAt,

                User = userDto,

                IsNewUser = false
            });
    }
}