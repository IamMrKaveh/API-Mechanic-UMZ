using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketCreatedEvent(
    TicketId TicketId,
    UserId CustomerId,
    string Subject,
    string Category,
    ValueObjects.TicketPriority Priority) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}