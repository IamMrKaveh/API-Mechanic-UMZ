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
    public string CodeHash { get; private set; } = default!;
    public OtpPurpose Purpose { get; private set; }

    /// <summary>
    /// The number this challenge was issued for, when it is not the number the
    /// user already owns (phone-change flow). Null for every other purpose.
    /// </summary>
    public PhoneNumber? TargetPhoneNumber { get; private set; }

    public bool IsVerified { get; private set; }
    public int VerificationAttempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;
    public bool IsUsable(DateTime now) => !IsVerified && !IsExpired(now) && !IsLockedOut;
    public bool IsLockedOut => VerificationAttempts >= MaxVerificationAttempts;
    public int RemainingAttempts => Math.Max(0, MaxVerificationAttempts - VerificationAttempts);

    public TimeSpan? GetTimeUntilExpiry(DateTime now)
    {
        if (IsExpired(now)) return null;
        var remaining = ExpiresAt - now;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public static UserOtp Create(
        UserId userId,
        OtpCode code,
        OtpPurpose purpose,
        TimeSpan validity,
        DateTime now)
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
            CodeHash = code.ToHash(),
            Purpose = purpose,
            IsVerified = false,
            VerificationAttempts = 0,
            ExpiresAt = now.Add(validity),
            CreatedAt = now
        };

        otp.RaiseDomainEvent(new OtpGeneratedEvent(otp.Id, userId, purpose, otp.ExpiresAt));
        return otp;
    }

    /// <summary>
    /// Issues a phone-verification challenge for a number the user wants to move
    /// to, rather than for the number they already own.
    /// </summary>
    /// <remarks>
    /// Binding the target to the OTP is what proves ownership of the *new*
    /// number: without it any valid code for the account could be replayed
    /// against a different number. Validity rules are unchanged and come from
    /// <see cref="Create"/>.
    /// </remarks>
    public static UserOtp CreateForPhoneChange(
        UserId userId,
        OtpCode code,
        PhoneNumber target,
        TimeSpan validity,
        DateTime now)
    {
        Guard.Against.Null(target, nameof(target));

        var otp = Create(userId, code, OtpPurpose.PhoneVerification, validity, now);
        otp.TargetPhoneNumber = target;

        return otp;
    }

    public void Verify(OtpCode providedCode, DateTime now)
    {
        if (IsVerified)
            throw new OtpAlreadyVerifiedException(Id);

        if (IsExpired(now))
            throw new OtpExpiredException(Id);

        if (IsLockedOut)
            throw new OtpMaxAttemptsExceededException(Id, MaxVerificationAttempts);

        VerificationAttempts++;

        if (!providedCode.MatchesHash(CodeHash))
        {
            RaiseDomainEvent(new OtpVerificationFailedEvent(Id, UserId, Purpose, VerificationAttempts, RemainingAttempts));
            throw new InvalidOtpCodeException(Id);
        }

        IsVerified = true;
        VerifiedAt = now;
        RaiseDomainEvent(new OtpVerifiedEvent(Id, UserId, Purpose));
    }

    public void MarkExpired(DateTime now)
    {
        if (IsVerified || IsExpired(now) is false)
            return;

        RaiseDomainEvent(new OtpExpiredEvent(Id, UserId, Purpose));
    }
}
