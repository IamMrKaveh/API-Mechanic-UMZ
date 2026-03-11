namespace Domain.User.Events;

public class SessionCreatedEvent(int userId, int sessionId, string ipAddress) : DomainEvent
{
    public int UserId { get; } = userId;
    public int SessionId { get; } = sessionId;
    public string IpAddress { get; } = ipAddress;
}