using Domain.Common.Abstractions;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketClosedEvent(
    TicketId TicketId,
    UserId CustomerId) : IDomainEvent;