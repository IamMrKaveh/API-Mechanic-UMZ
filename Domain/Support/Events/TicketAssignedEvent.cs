using Domain.Common.Abstractions;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketAssignedEvent(
    TicketId TicketId,
    UserId CustomerId,
    UserId? PreviousAgentId,
    UserId NewAgentId) : IDomainEvent;