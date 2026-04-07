using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketAssignedEvent(
    TicketId ticketId,
    UserId customerId,
    UserId? previousAgentId,
    UserId newAgentId) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId CustomerId { get; } = customerId;
    public UserId? PreviousAgentId { get; } = previousAgentId;
    public UserId NewAgentId { get; } = newAgentId;
}