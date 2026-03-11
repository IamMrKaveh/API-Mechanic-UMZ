namespace Domain.User.Events;

public class UserDemotedFromAdminEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}