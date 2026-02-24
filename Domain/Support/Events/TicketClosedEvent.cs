namespace Domain.Support.Events;

public sealed class TicketClosedEvent : DomainEvent
{
    public int TicketId { get; }

    public TicketClosedEvent(int ticketId)
    {
        TicketId = ticketId;
    }
}