using Application.Auth.Features.Shared;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler(
    IOtpRepository otpRepository,
    IUserRepository userRepository,
    ISessionService sessionService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    public async Task<ServiceResult<AuthResult>> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var ipAddress = IpAddress.Create(request.IpAddress);
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
        catch (Exception ex)
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<AuthResult>.Failure(ex.Message);
        }

        otpRepository.Update(otp);
        await unitOfWork.SaveChangesAsync(ct);

        var authResult = await sessionService.CreateSessionAsync(user.Id, ipAddress, request.UserAgent, ct);
        if (!authResult.IsSuccess)
            return ServiceResult<AuthResult>.Failure(authResult.Error);

        await auditService.LogSecurityEventAsync(
            "VerifyOtp",
            $"OTP برای شماره {request.PhoneNumber} تأیید شد.",
            ipAddress,
            ct: ct);

        var (accessToken, refreshToken, expiresAt, _) = authResult.Value;

        return ServiceResult<AuthResult>.Success(new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = expiresAt,
        });
    }
}