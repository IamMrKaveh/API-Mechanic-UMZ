using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketPriorityChangedEvent(
    TicketId TicketId,
    UserId CustomerId,
    TicketPriority PreviousPriority,
    TicketPriority NewPriority) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}