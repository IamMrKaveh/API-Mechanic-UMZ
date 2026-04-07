using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketReopenedEvent(
    TicketId ticketId,
    UserId customerId) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId CustomerId { get; } = customerId;
}