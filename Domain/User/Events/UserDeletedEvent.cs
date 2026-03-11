namespace Domain.User.Events;

public class UserDeletedEvent(int userId, int? deletedBy) : DomainEvent
{
    public int UserId { get; } = userId;
    public int? DeletedBy { get; } = deletedBy;
}