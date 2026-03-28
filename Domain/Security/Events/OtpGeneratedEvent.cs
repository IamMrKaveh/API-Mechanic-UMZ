using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record OtpGeneratedEvent(
    UserOtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose,
    DateTime ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}