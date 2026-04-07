using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class OtpVerifiedEvent(
    OtpId otpId,
    UserId userId,
    OtpPurpose purpose) : DomainEvent
{
    public OtpId OtpId { get; } = otpId;
    public UserId UserId { get; } = userId;
    public OtpPurpose Purpose { get; } = purpose;
}