namespace Domain.User.Events;

public class UserRestoredEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}