namespace Domain.User.Events;

public class SessionRevokedEvent(int userId, int sessionId) : DomainEvent
{
    public int UserId { get; } = userId;
    public int SessionId { get; } = sessionId;
}