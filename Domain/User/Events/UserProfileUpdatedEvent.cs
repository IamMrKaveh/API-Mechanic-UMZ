namespace Domain.User.Events;

public class UserProfileUpdatedEvent : DomainEvent
{
    public int UserId { get; }

    public UserProfileUpdatedEvent(int userId)
    {
        UserId = userId;
    }
}