namespace Domain.User.Events;

public class UserDemotedFromAdminEvent : DomainEvent
{
    public int UserId { get; }

    public UserDemotedFromAdminEvent(int userId)
    {
        UserId = userId;
    }
}