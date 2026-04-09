using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class SessionRevokedEvent(
    SessionId sessionId,
    UserId userId,
    SessionRevocationReason reason) : DomainEvent
{
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;
    public SessionRevocationReason Reason { get; } = reason;
}