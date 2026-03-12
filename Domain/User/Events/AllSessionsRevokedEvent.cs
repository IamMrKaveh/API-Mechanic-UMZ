namespace Domain.User.Events;

public class AllSessionsRevokedEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}