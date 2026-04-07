using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class SessionRevokedEvent(
    UserSessionId sessionId,
    UserId userId,
    SessionRevocationReason reason) : DomainEvent
{
    public UserSessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;
    public SessionRevocationReason Reason { get; } = reason;
}