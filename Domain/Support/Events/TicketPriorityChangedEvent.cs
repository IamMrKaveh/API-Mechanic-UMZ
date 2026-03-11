namespace Domain.Support.Events;

public sealed class TicketPriorityChangedEvent(int ticketId, string oldPriority, string newPriority) : DomainEvent
{
    public int TicketId { get; } = ticketId;
    public string OldPriority { get; } = oldPriority;
    public string NewPriority { get; } = newPriority;
}