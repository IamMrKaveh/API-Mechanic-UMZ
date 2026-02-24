namespace Domain.User.Events;

public sealed class UserLoggedInEvent : DomainEvent
{
    public int UserId { get; }

    public UserLoggedInEvent(int userId)
    {
        UserId = userId;
    }
}