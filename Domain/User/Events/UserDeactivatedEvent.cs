namespace Domain.User.Events;

public class UserDeactivatedEvent : DomainEvent
{
    public int UserId { get; }

    public UserDeactivatedEvent(int userId)
    {
        UserId = userId;
    }
}