namespace Domain.Support.Events;

public sealed record TicketMessageEditedEvent(
    TicketMessageId MessageId,
    TicketId TicketId,
    UserId SenderId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}