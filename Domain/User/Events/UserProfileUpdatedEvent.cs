namespace Domain.User.Events;

public sealed record UserProfileUpdatedEvent(
    UserId UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}