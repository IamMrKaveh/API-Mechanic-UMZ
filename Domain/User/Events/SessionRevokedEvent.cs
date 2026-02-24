namespace Domain.User.Events;

public class SessionRevokedEvent : DomainEvent
{
    public int UserId { get; }
    public int SessionId { get; }

    public SessionRevokedEvent(int userId, int sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }
}