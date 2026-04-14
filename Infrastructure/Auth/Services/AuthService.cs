using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Options;
using MapsterMapper;

namespace Infrastructure.Auth.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    ISessionRepository sessionRepository,
    ISessionService sessionService,
    IJwtTokenGenerator jwtTokenGenerator,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> authOptions,
    IMapper mapper,
    IAuditService auditService) : IAuthService
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    public async Task<ServiceResult> RequestOtpAsync(
        PhoneNumber phoneNumber,
        IpAddress ipAddress,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);

        if (user is null)
        {
            user = Domain.User.Aggregates.User.RegisterByPhone(phoneNumber);

            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await otpService.ValidateRateLimitAsync(user.Id, OtpPurpose.Login, ct))
            return ServiceResult.Failure("تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً بعداً تلاش کنید.");

        await otpRepository.InvalidateAllActiveByUserIdAsync(user.Id, OtpPurpose.Login, ct);

        var otpCode = OtpCode.Generate(_authOptions.OtpLength);
        var validity = TimeSpan.FromMinutes(_authOptions.OtpExpirationMinutes);

        var otp = UserOtp.Create(user.Id, otpCode, OtpPurpose.Login, validity);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await otpService.SendOtpAsync(phoneNumber, otpCode, OtpPurpose.Login, ct);

        await auditService.LogSecurityEventAsync("RequestOtp", $"OTP requested for {phoneNumber.Value}", ipAddress, user.Id, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> VerifyOtpAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByPhoneNumberAsync(phoneNumber, ct);
        if (user is null)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.NotFound("کاربر یافت نشد.");

        var otp = await otpRepository.GetLatestActiveByUserIdAsync(user.Id, OtpPurpose.Login, ct);
        if (otp is null)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Validation("کد تأیید نامعتبر یا منقضی شده است.");

        try
        {
            otp.Verify(code.Value);
        }
        catch
        {
            otpRepository.Update(otp);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Validation("کد تأیید نادرست است.");
        }

        otpRepository.Update(otp);

        var isNewUser = !user.IsEmailVerified && user.CreatedAt > DateTime.UtcNow.AddMinutes(-5);

        if (!user.IsEmailVerified)
            user.VerifyEmail();

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var sessionResult = await sessionService.CreateSessionAsync(user.Id, ipAddress, userAgent, ct);

        if (!sessionResult.IsSuccess)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Failure(sessionResult.Error!);

        var userDto = mapper.Map<UserProfileDto>(user);

        await auditService.LogSecurityEventAsync("VerifyOtp", "OTP verified successfully", ipAddress, user.Id, ct);

        return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(
            (accessToken, sessionResult.Value!, userDto, isNewUser));
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> RefreshTokenAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var existingSession = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);
        if (existingSession is null || !existingSession.IsActive)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized("جلسه نامعتبر است.");

        var userId = existingSession.UserId;

        var sessionResult = await sessionService.RefreshSessionAsync(refreshToken, ipAddress, ct);
        if (!sessionResult.IsSuccess)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized(sessionResult.Error!);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.NotFound("کاربر یافت نشد.");

        if (!user.IsActive)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized("حساب کاربری غیرفعال است.");

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var userDto = mapper.Map<UserProfileDto>(user);

        return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(
            (accessToken, sessionResult.Value!, userDto, false));
    }

    public async Task<ServiceResult> LogoutAsync(
        UserId userId,
        RefreshToken? refreshToken,
        CancellationToken ct = default)
    {
        if (refreshToken is not null)
        {
            var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);
            if (session is not null && session.IsActive)
            {
                await sessionService.RevokeSessionAsync(session.Id, ct);
            }
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> LogoutAllAsync(UserId userId, CancellationToken ct = default)
    {
        await sessionService.RevokeAllSessionsAsync(userId, ct);
        return ServiceResult.Success();
    }
}