using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.Security.Exceptions;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Aggregates;

public sealed class UserOtp : AggregateRoot<OtpId>
{
    private const int MaxVerificationAttempts = 5;

    private UserOtp()
    { }

    public UserId UserId { get; private set; } = default!;
    public OtpCode Code { get; private set; } = default!;
    public OtpPurpose Purpose { get; private set; }
    public bool IsVerified { get; private set; }
    public int VerificationAttempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsable => !IsVerified && !IsExpired && !IsLockedOut;
    public bool IsLockedOut => VerificationAttempts >= MaxVerificationAttempts;
    public int RemainingAttempts => Math.Max(0, MaxVerificationAttempts - VerificationAttempts);

    public TimeSpan? GetTimeUntilExpiry()
    {
        if (IsExpired) return null;
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public static UserOtp Create(
        UserId userId,
        OtpCode code,
        OtpPurpose purpose,
        TimeSpan validity)
    {
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(code, nameof(code));

        if (validity <= TimeSpan.Zero)
            throw new DomainException("مدت اعتبار OTP باید بزرگتر از صفر باشد.");

        if (validity > TimeSpan.FromMinutes(30))
            throw new DomainException("مدت اعتبار OTP نمی‌تواند بیش از ۳۰ دقیقه باشد.");

        var otp = new UserOtp
        {
            Id = OtpId.NewId(),
            UserId = userId,
            Code = code,
            Purpose = purpose,
            IsVerified = false,
            VerificationAttempts = 0,
            ExpiresAt = DateTime.UtcNow.Add(validity),
            CreatedAt = DateTime.UtcNow
        };

        otp.RaiseDomainEvent(new OtpGeneratedEvent(otp.Id, userId, purpose, otp.ExpiresAt));
        return otp;
    }

    public void Verify(string providedCode)
    {
        if (IsVerified)
            throw new OtpAlreadyVerifiedException(Id);

        if (IsExpired)
            throw new OtpExpiredException(Id);

        if (IsLockedOut)
            throw new OtpMaxAttemptsExceededException(Id, MaxVerificationAttempts);

        VerificationAttempts++;

        if (!Code.Matches(providedCode))
        {
            RaiseDomainEvent(new OtpVerificationFailedEvent(Id, UserId, Purpose, VerificationAttempts, RemainingAttempts));
            throw new InvalidOtpCodeException(Id);
        }

        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OtpVerifiedEvent(Id, UserId, Purpose));
    }

    public void MarkExpired()
    {
        if (IsVerified || IsExpired)
            return;

        RaiseDomainEvent(new OtpExpiredEvent(Id, UserId, Purpose));
    }
}