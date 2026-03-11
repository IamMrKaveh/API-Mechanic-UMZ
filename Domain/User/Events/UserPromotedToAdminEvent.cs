namespace Domain.User.Events;

public class UserPromotedToAdminEvent(int userId) : DomainEvent
{
    public int UserId { get; } = userId;
}