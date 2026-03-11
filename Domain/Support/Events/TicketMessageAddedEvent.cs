using Domain.Common.Abstractions;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketMessageAddedEvent(
    TicketId TicketId,
    TicketMessageId MessageId,
    UserId CustomerId,
    UserId SenderId,
    TicketMessageSenderType SenderType,
    int NewMessageCount) : IDomainEvent;