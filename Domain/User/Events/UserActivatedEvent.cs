namespace Domain.User.Events;

public class UserActivatedEvent : DomainEvent
{
    public int UserId { get; }

    public UserActivatedEvent(int userId)
    {
        UserId = userId;
    }
}