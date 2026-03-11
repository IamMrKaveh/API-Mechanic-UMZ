namespace Domain.Support.Events;

public sealed class TicketSubjectUpdatedEvent(int ticketId, string newSubject) : DomainEvent
{
    public int TicketId { get; } = ticketId;
    public string NewSubject { get; } = newSubject;
}