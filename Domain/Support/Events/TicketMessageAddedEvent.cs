namespace Domain.Support.Events;

public sealed class TicketMessageAddedEvent : DomainEvent
{
    public int TicketId { get; }
    public string MessagePreview { get; }

    public TicketMessageAddedEvent(int ticketId, string messagePreview)
    {
        TicketId = ticketId;
        MessagePreview = messagePreview;
    }
}