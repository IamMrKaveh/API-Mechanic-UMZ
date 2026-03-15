using Domain.Support.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketSubjectUpdatedEvent(
    TicketId TicketId,
    string NewSubject) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}