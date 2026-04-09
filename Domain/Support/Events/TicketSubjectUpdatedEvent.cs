using Domain.Support.ValueObjects;

namespace Domain.Support.Events;

public sealed class TicketSubjectUpdatedEvent(
    TicketId ticketId,
    string newSubject) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public string NewSubject { get; } = newSubject;
}