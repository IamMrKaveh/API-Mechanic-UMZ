using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class OtpGeneratedEvent(
    OtpId otpId,
    UserId userId,
    OtpPurpose purpose,
    DateTime expiresAt) : DomainEvent
{
    public OtpId OtpId { get; } = otpId;
    public UserId UserId { get; } = userId;
    public OtpPurpose Purpose { get; } = purpose;
    public DateTime ExpiresAt { get; } = expiresAt;
}