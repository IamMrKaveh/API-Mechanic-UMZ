namespace Domain.User.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string FirstName,
    string LastName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}