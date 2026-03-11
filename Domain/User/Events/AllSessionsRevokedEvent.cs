namespace Domain.User.Events;

public class AllSessionsRevokedEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}