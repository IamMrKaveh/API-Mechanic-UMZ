namespace Domain.User.Events;

public class AllSessionsRevokedEvent : DomainEvent
{
    public int UserId { get; }

    public AllSessionsRevokedEvent(int userId)
    {
        UserId = userId;
    }
}