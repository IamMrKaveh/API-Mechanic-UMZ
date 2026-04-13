using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.Security.Exceptions;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Aggregates;

public sealed class UserSession : AggregateRoot<SessionId>
{
    private const int MaxSessionDurationDays = 90;

    private UserSession()
    { }

    public UserId UserId { get; private set; } = default!;
    public RefreshToken RefreshToken { get; private set; } = default!;
    public DeviceInfo DeviceInfo { get; private set; } = default!;
    public IpAddress IpAddress { get; private set; } = default!;
    public bool IsRevoked { get; private set; }
    public SessionRevocationReason? RevocationReason { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public TimeSpan? GetTimeUntilExpiry()
    {
        if (IsExpired) return null;
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public TimeSpan GetSessionDuration()
    {
        var endTime = RevokedAt ?? DateTime.UtcNow;
        return endTime - CreatedAt;
    }

    public static UserSession Create(
        SessionId id,
        UserId userId,
        RefreshToken refreshToken,
        DeviceInfo deviceInfo,
        IpAddress ipAddress,
        DateTime expiresAt)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(refreshToken, nameof(refreshToken));
        Guard.Against.Null(deviceInfo, nameof(deviceInfo));
        Guard.Against.Null(ipAddress, nameof(ipAddress));

        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("تاریخ انقضای نشست باید در آینده باشد.");

        if (expiresAt > DateTime.UtcNow.AddDays(MaxSessionDurationDays))
            throw new DomainException($"مدت نشست نمی‌تواند بیش از {MaxSessionDurationDays} روز باشد.");

        var session = new UserSession
        {
            Id = id,
            UserId = userId,
            RefreshToken = refreshToken,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IsRevoked = false,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        session.RaiseDomainEvent(new SessionCreatedEvent(id, userId, deviceInfo, ipAddress, expiresAt));
        return session;
    }

    public void RecordActivity()
    {
        if (!IsActive)
            return;

        LastActivityAt = DateTime.UtcNow;
    }

    public void Revoke(SessionRevocationReason reason = SessionRevocationReason.UserRequested)
    {
        if (IsRevoked)
            return;

        if (IsExpired)
            throw new SessionExpiredException(Id);

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        RaiseDomainEvent(new SessionRevokedEvent(Id, UserId, reason));
    }

    public void MarkExpired()
    {
        if (IsRevoked)
            return;

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = SessionRevocationReason.Expired;
        RaiseDomainEvent(new SessionExpiredEvent(Id, UserId));
    }

    public bool ValidateRefreshToken(string token)
    {
        if (!IsActive)
            return false;

        return RefreshToken.Matches(token);
    }
}