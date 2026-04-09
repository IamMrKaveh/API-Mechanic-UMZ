using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class SessionExpiredEvent(
    SessionId sessionId,
    UserId userId) : DomainEvent
{
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;
}