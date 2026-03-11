using Domain.Common.Abstractions;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketMessageEditedEvent(
    TicketMessageId MessageId,
    TicketId TicketId,
    UserId SenderId) : IDomainEvent;