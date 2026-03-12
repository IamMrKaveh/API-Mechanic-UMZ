namespace Domain.Support.Events;

public sealed record TicketStatusChangedEvent(
    TicketId TicketId,
    UserId CustomerId,
    ValueObjects.TicketStatus PreviousStatus,
    ValueObjects.TicketStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}