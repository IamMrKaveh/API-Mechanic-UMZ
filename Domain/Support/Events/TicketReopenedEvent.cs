namespace Domain.Support.Events;

public sealed class TicketReopenedEvent(int ticketId) : DomainEvent
{
    public int TicketId { get; } = ticketId;
}