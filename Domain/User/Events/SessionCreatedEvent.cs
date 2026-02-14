namespace Domain.User.Events;

public class SessionCreatedEvent : DomainEvent
{
    public int UserId { get; }
    public int SessionId { get; }
    public string IpAddress { get; }

    public SessionCreatedEvent(int userId, int sessionId, string ipAddress)
    {
        UserId = userId;
        SessionId = sessionId;
        IpAddress = ipAddress;
    }
}