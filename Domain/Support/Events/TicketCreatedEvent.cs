using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketCreatedEvent(
    TicketId ticketId,
    UserId customerId,
    string subject,
    string category,
    TicketPriority priority) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId CustomerId { get; } = customerId;
    public string Subject { get; } = subject;
    public string Category { get; } = category;
    public TicketPriority Priority { get; } = priority;
}