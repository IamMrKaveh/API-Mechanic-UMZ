namespace Domain.Support.Events;

public sealed class TicketAnsweredEvent(int ticketId, int? adminId) : DomainEvent
{
    public int TicketId { get; } = ticketId;
    public int? AdminId { get; } = adminId;
}