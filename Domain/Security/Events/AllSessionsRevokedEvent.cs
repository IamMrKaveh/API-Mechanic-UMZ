using Domain.Security.Enums;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class AllSessionsRevokedEvent(
    UserId userId,
    SessionRevocationReason reason,
    int revokedCount) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public SessionRevocationReason Reason { get; } = reason;
    public int RevokedCount { get; } = revokedCount;
}