namespace Domain.Support.Events;

public sealed record TicketPriorityChangedEvent(
    TicketId TicketId,
    UserId CustomerId,
    ValueObjects.TicketPriority PreviousPriority,
    ValueObjects.TicketPriority NewPriority) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}