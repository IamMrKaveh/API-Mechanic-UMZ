namespace Domain.User.Events;

public sealed class UserLoggedInEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}