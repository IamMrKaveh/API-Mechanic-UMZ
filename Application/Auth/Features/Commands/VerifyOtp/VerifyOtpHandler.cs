using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    ISessionService sessionService,
    IJwtTokenGenerator jwtTokenGenerator,
    ICurrentUserService currentUser,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    IOptions<JwtOptions> jwtOptions)
    : ICommandHandler<VerifyOtpCommand, AuthResult>
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<ServiceResult<AuthResult>> Handle(
        VerifyOtpCommand request,
        CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var ipAddress = IpAddress.Create(currentUser.IpAddress ?? IpAddress.Unknown.Value);
        var otpCode = OtpCode.Create(request.Code);

        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);

        if (user is null)
            return ServiceResult<AuthResult>.Failure("کاربری با این شماره یافت نشد.");

        var otp = await otpRepository.GetLatestActiveByUserIdAsync(user.Id, request.Purpose, ct);

        if (otp is null)
            return ServiceResult<AuthResult>.Failure("کد OTP فعالی یافت نشد.");

        try
        {
            otp.Verify(otpCode);
        }
        catch (DomainException ex)
        {
            otpRepository.Update(otp);
            return ServiceResult<AuthResult>.Failure(ex.Message);
        }

        otpRepository.Update(otp);

        var deviceDescriptor = !string.IsNullOrWhiteSpace(request.DeviceInfo)
            ? request.DeviceInfo
            : currentUser.UserAgent;

        var sessionResult = await sessionService.CreateSessionAsync(
            user.Id,
            ipAddress,
            deviceDescriptor,
            ct);

        if (sessionResult.IsSuccess is false)
            return ServiceResult<AuthResult>.Failure(sessionResult.Error);

        await auditService.LogSecurityEventAsync(
            "VerifyOtp",
            $"OTP برای شماره {request.PhoneNumber} تأیید شد.",
            ipAddress,
            user.Id,
            ct);

        var refreshSession = sessionResult.Value!;
        var newSessionId = SessionId.From(refreshSession.SessionId);
        var jwtAccessToken = jwtTokenGenerator.GenerateAccessToken(user, newSessionId);
        var userDto = user.Adapt<UserProfileDto>();

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = jwtAccessToken,
            RefreshToken = refreshSession.RefreshToken,
            AccessTokenExpiresAt = dateTimeProvider.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAt = refreshSession.ExpiresAt,
            User = userDto,
            IsNewUser = false
        });
    }
}