namespace Domain.User.Events;

public class UserRestoredEvent : DomainEvent
{
    public int UserId { get; }

    public UserRestoredEvent(int userId)
    {
        UserId = userId;
    }
}