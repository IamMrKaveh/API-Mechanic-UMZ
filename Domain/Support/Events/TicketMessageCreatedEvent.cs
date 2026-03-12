namespace Domain.Support.Events;

public sealed record TicketMessageCreatedEvent(
    TicketMessageId MessageId,
    TicketId TicketId,
    UserId SenderId,
    TicketMessageSenderType SenderType) : IDomainEvent;