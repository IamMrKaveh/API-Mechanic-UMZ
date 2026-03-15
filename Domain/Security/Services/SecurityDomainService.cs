using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Exceptions;
using Domain.Security.Results;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Services;

public sealed class SecurityDomainService
{
    private const int MaxActiveSessionsPerUser = 10;
    private const int MaxOtpRequestsPerWindow = 5;
    private static readonly TimeSpan OtpRateLimitWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultOtpValidity = TimeSpan.FromMinutes(5);

    public OtpGenerationResult GenerateOtp(
        UserId userId,
        OtpPurpose purpose,
        int recentOtpCount,
        int codeLength = 6)
    {
        Guard.Against.Null(userId, nameof(userId));

        if (recentOtpCount >= MaxOtpRequestsPerWindow)
        {
            return OtpGenerationResult.RateLimited(
                userId,
                purpose,
                OtpRateLimitWindow);
        }

        var otpId = UserOtpId.NewId();
        var code = OtpCode.Generate(codeLength);

        var otp = UserOtp.Create(otpId, userId, code, purpose, DefaultOtpValidity);

        return OtpGenerationResult.Success(otp);
    }

    public OtpVerificationResult VerifyOtp(
        UserOtp? otp,
        string providedCode)
    {
        if (otp is null)
            return OtpVerificationResult.Failed("کد OTP یافت نشد.");

        if (!otp.IsUsable)
        {
            if (otp.IsVerified)
                return OtpVerificationResult.Failed("کد OTP قبلاً استفاده شده است.");
            if (otp.IsExpired)
                return OtpVerificationResult.Failed("کد OTP منقضی شده است.");
            if (otp.IsLockedOut)
                return OtpVerificationResult.Failed("تعداد تلاش‌های مجاز به پایان رسیده است.");
            return OtpVerificationResult.Failed("کد OTP قابل استفاده نیست.");
        }

        try
        {
            otp.Verify(providedCode);
            return OtpVerificationResult.Verified(otp);
        }
        catch (InvalidOtpCodeException)
        {
            return OtpVerificationResult.InvalidCode(otp.RemainingAttempts);
        }
    }

    public SessionCreationResult CreateSession(
        UserId userId,
        string deviceInfo,
        string ipAddress,
        int activeSessionCount,
        TimeSpan sessionDuration)
    {
        Guard.Against.Null(userId, nameof(userId));

        if (activeSessionCount >= MaxActiveSessionsPerUser)
        {
            return SessionCreationResult.MaxSessionsExceeded(
                userId,
                MaxActiveSessionsPerUser);
        }

        var sessionId = UserSessionId.NewId();
        var refreshToken = RefreshToken.Generate();
        var deviceInfoVo = DeviceInfo.Create(deviceInfo);
        var ipAddressVo = Common.ValueObjects.IpAddress.Create(ipAddress);
        var expiresAt = DateTime.UtcNow.Add(sessionDuration);

        var session = UserSession.Create(
            sessionId,
            userId,
            refreshToken,
            deviceInfoVo,
            ipAddressVo,
            expiresAt);

        return SessionCreationResult.Success(session);
    }

    public int RevokeAllSessions(
        IEnumerable<UserSession> activeSessions,
        SessionRevocationReason reason)
    {
        Guard.Against.Null(activeSessions, nameof(activeSessions));

        var revokedCount = 0;

        foreach (var session in activeSessions)
        {
            if (session.IsActive)
            {
                session.Revoke(reason);
                revokedCount++;
            }
        }

        return revokedCount;
    }

    public int ExpireStaleSessions(IEnumerable<UserSession> sessions)
    {
        Guard.Against.Null(sessions, nameof(sessions));

        var expiredCount = 0;

        foreach (var session in sessions)
        {
            if (session.IsExpired && !session.IsRevoked)
            {
                session.MarkExpired();
                expiredCount++;
            }
        }

        return expiredCount;
    }

    public SessionValidationResult ValidateSession(UserSession? session)
    {
        if (session is null)
            return SessionValidationResult.NotFound();

        if (session.IsRevoked)
            return SessionValidationResult.Revoked(session.RevocationReason);

        if (session.IsExpired)
            return SessionValidationResult.Expired();

        return SessionValidationResult.Valid(session);
    }
}