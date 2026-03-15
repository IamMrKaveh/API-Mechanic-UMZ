using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserOtpVerificationFailedEvent(
    UserOtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose,
    int AttemptNumber,
    int RemainingAttempts) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}