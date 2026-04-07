using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class OtpGeneratedEvent(
    UserOtpId otpId,
    UserId userId,
    OtpPurpose purpose,
    DateTime expiresAt) : DomainEvent
{
    public UserOtpId OtpId { get; } = otpId;
    public UserId UserId { get; } = userId;
    public OtpPurpose Purpose { get; } = purpose;
    public DateTime ExpiresAt { get; } = expiresAt;
}