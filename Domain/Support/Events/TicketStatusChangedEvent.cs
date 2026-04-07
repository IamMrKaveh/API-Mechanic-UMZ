using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketStatusChangedEvent(
    TicketId ticketId,
    UserId customerId,
    TicketStatus previousStatus,
    TicketStatus newStatus) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId CustomerId { get; } = customerId;
    public TicketStatus PreviousStatus { get; } = previousStatus;
    public TicketStatus NewStatus { get; } = newStatus;
}