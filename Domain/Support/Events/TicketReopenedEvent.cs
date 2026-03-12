namespace Domain.Support.Events;

public sealed record TicketReopenedEvent(
    TicketId TicketId,
    UserId CustomerId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}