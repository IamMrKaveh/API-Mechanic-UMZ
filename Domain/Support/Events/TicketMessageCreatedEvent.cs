using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed class TicketMessageCreatedEvent(
    TicketMessageId MessageId,
    TicketId TicketId,
    UserId SenderId,
    TicketMessageSenderType SenderType) : DomainEvent
{
    public TicketMessageId MessageId { get; } = MessageId;
    public TicketId TicketId { get; } = TicketId;
    public UserId SenderId { get; } = SenderId;
    public TicketMessageSenderType SenderType { get; } = SenderType;
}