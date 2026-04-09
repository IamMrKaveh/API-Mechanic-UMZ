using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed class TicketMessageEditedEvent(
    TicketMessageId messageId,
    TicketId ticketId,
    UserId senderId) : DomainEvent
{
    public TicketMessageId MessageId { get; } = messageId;
    public TicketId TicketId { get; } = ticketId;
    public UserId SenderId { get; } = senderId;
}