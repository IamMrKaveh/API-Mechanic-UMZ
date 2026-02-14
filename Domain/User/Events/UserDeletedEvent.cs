namespace Domain.User.Events;

public class UserDeletedEvent : DomainEvent
{
    public int UserId { get; }
    public int? DeletedBy { get; }

    public UserDeletedEvent(int userId, int? deletedBy)
    {
        UserId = userId;
        DeletedBy = deletedBy;
    }
}