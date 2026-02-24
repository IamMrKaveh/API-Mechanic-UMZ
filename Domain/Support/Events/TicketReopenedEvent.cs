namespace Domain.Support.Events;

public sealed class TicketReopenedEvent : DomainEvent
{
    public int TicketId { get; }

    public TicketReopenedEvent(int ticketId)
    {
        TicketId = ticketId;
    }
}