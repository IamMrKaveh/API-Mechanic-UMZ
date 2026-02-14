namespace Domain.Support.Events;

public sealed class TicketCreatedEvent : DomainEvent
{
    public int TicketId { get; }
    public int UserId { get; }

    public TicketCreatedEvent(int ticketId, int userId)
    {
        TicketId = ticketId;
        UserId = userId;
    }
}