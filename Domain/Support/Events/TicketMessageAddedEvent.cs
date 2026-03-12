namespace Domain.Support.Events;

public sealed record TicketMessageAddedEvent(
    TicketId TicketId,
    TicketMessageId MessageId,
    UserId CustomerId,
    UserId SenderId,
    TicketMessageSenderType SenderType,
    int NewMessageCount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}