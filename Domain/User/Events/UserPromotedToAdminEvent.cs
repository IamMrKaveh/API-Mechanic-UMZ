namespace Domain.User.Events;

public class UserPromotedToAdminEvent : DomainEvent
{
    public int UserId { get; }

    public UserPromotedToAdminEvent(int userId)
    {
        UserId = userId;
    }
}