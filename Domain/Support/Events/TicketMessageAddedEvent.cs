using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed class TicketMessageAddedEvent(
    TicketId ticketId,
    TicketMessageId messageId,
    UserId customerId,
    UserId senderId,
    TicketMessageSenderType senderType,
    int newMessageCount) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public TicketMessageId MessageId { get; } = messageId;
    public UserId CustomerId { get; } = customerId;
    public UserId SenderId { get; } = senderId;
    public TicketMessageSenderType SenderType { get; } = senderType;
    public int NewMessageCount { get; } = newMessageCount;
}