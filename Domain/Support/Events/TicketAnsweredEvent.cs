namespace Domain.Support.Events;

public sealed class TicketAnsweredEvent : DomainEvent
{
    public int TicketId { get; }
    public int? AdminId { get; }

    public TicketAnsweredEvent(int ticketId, int? adminId)
    {
        TicketId = ticketId;
        AdminId = adminId;
    }
}