namespace Domain.User.Events;

public class UserLockedOutEvent : DomainEvent
{
    public int UserId { get; }
    public DateTime LockoutEndTime { get; }

    public UserLockedOutEvent(int userId, DateTime lockoutEndTime)
    {
        UserId = userId;
        LockoutEndTime = lockoutEndTime;
    }
}