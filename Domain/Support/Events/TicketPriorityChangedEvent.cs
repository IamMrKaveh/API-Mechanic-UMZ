using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketPriorityChangedEvent(
    TicketId ticketId,
    UserId customerId,
    TicketPriority previousPriority,
    TicketPriority newPriority) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId CustomerId { get; } = customerId;
    public TicketPriority PreviousPriority { get; } = previousPriority;
    public TicketPriority NewPriority { get; } = newPriority;
}