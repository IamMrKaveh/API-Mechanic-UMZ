namespace Domain.Support.Events;

public sealed class TicketSubjectUpdatedEvent : DomainEvent
{
    public int TicketId { get; }
    public string NewSubject { get; }

    public TicketSubjectUpdatedEvent(int ticketId, string newSubject)
    {
        TicketId = ticketId;
        NewSubject = newSubject;
    }
}