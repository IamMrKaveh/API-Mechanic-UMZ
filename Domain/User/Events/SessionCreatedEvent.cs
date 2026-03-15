using Domain.User.ValueObjects;

namespace Domain.User.Events;

public class SessionCreatedEvent(UserId userId, int sessionId, string ipAddress) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public int SessionId { get; } = sessionId;
    public string IpAddress { get; } = ipAddress;
}