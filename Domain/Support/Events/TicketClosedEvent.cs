namespace Domain.Support.Events;

public sealed record TicketClosedEvent(
    TicketId TicketId,
    UserId CustomerId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}