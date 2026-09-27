using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketAnsweredEvent(
    TicketId TicketId,
    UserId AdminId) : DomainEvent;
