namespace Domain.User.Events;

public class UserLockedOutEvent(int userId, DateTime lockoutEndTime) : DomainEvent
{
    public int UserId { get; } = userId;
    public DateTime LockoutEndTime { get; } = lockoutEndTime;
}