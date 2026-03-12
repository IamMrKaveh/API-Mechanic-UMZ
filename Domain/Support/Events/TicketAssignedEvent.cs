namespace Domain.Support.Events;

public sealed record TicketAssignedEvent(
    TicketId TicketId,
    UserId CustomerId,
    UserId? PreviousAgentId,
    UserId NewAgentId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}