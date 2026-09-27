using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketStatusChangedEvent(
    TicketId TicketId,
    UserId CustomerId,
    TicketStatus PreviousStatus,
    TicketStatus NewStatus) : DomainEvent;
