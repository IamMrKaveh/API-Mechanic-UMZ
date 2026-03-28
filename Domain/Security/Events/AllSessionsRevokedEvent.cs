using Domain.Security.Enums;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record AllSessionsRevokedEvent(
    UserId UserId,
    SessionRevocationReason Reason,
    int RevokedCount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}