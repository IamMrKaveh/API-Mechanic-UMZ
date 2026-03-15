using Domain.User.ValueObjects;

namespace Domain.User.Events;

public class SessionRevokedEvent(UserId userId, int sessionId) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public int SessionId { get; } = sessionId;
}