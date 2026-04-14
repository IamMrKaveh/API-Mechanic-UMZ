using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Common.Interfaces;
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
    ISessionService sessionService,
    IJwtTokenGenerator jwtTokenGenerator,
    IOtpService otpService,
    IUnitOfWork unitOfWork,
    IOptions<AuthOptions> authOptions,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
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
            user = User.Register(
                UserId.NewId(),
                phoneNumber,
                null, null, null);

            await userRepository.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await otpService.ValidateRateLimitAsync(user.Id, OtpPurpose.Login, ct))
            return ServiceResult.Failure("تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً بعداً تلاش کنید.");

        await otpRepository.InvalidatePreviousOtpsAsync(user.Id, OtpPurpose.Login, ct);

        var otpCode = OtpCode.Generate(_authOptions.OtpLength);
        var expiresAt = DateTime.UtcNow.AddMinutes(_authOptions.OtpExpirationMinutes);
        var codeHash = otpService.HashOtp(otpCode);

        var otp = UserOtp.Create(
            OtpId.NewId(),
            user.Id,
            phoneNumber,
            codeHash,
            OtpPurpose.Login,
            expiresAt,
            _authOptions.MaxFailedOtpAttempts);

        await otpRepository.AddAsync(otp, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await otpService.SendOtpAsync(phoneNumber, otpCode, OtpPurpose.Login, ct);

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

        var otp = await otpRepository.GetActiveOtpAsync(user.Id, OtpPurpose.Login, ct);
        if (otp is null)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Validation("کد تأیید نامعتبر یا منقضی شده است.");

        var codeHash = otpService.HashOtp(code);
        var verificationResult = otp.Verify(codeHash);

        if (!verificationResult.IsVerified)
        {
            otpRepository.Update(otp);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Validation(
                verificationResult.FailureReason ?? "کد تأیید نادرست است.");
        }

        otpRepository.Update(otp);

        var isNewUser = !user.IsEmailVerified && user.CreatedAt > DateTime.UtcNow.AddMinutes(-5);
        user.MarkPhoneVerified();
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var sessionResult = await sessionService.CreateSessionAsync(user.Id, ipAddress, userAgent, ct);

        if (!sessionResult.IsSuccess)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Failure(sessionResult.Error!);

        var userDto = mapper.Map<UserProfileDto>(user);

        return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(
            (accessToken, sessionResult.Value!, userDto, isNewUser));
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> RefreshTokenAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var sessionResult = await sessionService.RefreshSessionAsync(refreshToken, ipAddress, ct);

        if (!sessionResult.IsSuccess)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized(sessionResult.Error!);

        var userId = UserId.From(sessionResult.Value!.UserId);
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
            var parts = refreshToken.Value.Split('.');
            if (parts.Length == 2)
            {
                var session = await ((ISessionRepository)sessionService).GetBySelectorAsync(parts[0], ct)
                    .ConfigureAwait(false);
                if (session is not null)
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