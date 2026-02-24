namespace Domain.User.Events;

public sealed class UserLoginFailedEvent : DomainEvent
{
    public int UserId { get; }
    public int FailedAttempts { get; }

    public UserLoginFailedEvent(int userId, int failedAttempts)
    {
        UserId = userId;
        FailedAttempts = failedAttempts;
    }
}