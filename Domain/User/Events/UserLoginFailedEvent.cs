namespace Domain.User.Events;

public sealed class UserLoginFailedEvent(int userId, int failedAttempts) : DomainEvent
{
    public int UserId { get; } = userId;
    public int FailedAttempts { get; } = failedAttempts;
}