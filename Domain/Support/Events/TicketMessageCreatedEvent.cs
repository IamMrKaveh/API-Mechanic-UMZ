using Domain.Common.Abstractions;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketMessageCreatedEvent(
    TicketMessageId MessageId,
    TicketId TicketId,
    UserId SenderId,
    TicketMessageSenderType SenderType) : IDomainEvent;