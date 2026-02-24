namespace Domain.Support.Events;

public sealed class TicketPriorityChangedEvent : DomainEvent
{
    public int TicketId { get; }
    public string OldPriority { get; }
    public string NewPriority { get; }

    public TicketPriorityChangedEvent(int ticketId, string oldPriority, string newPriority)
    {
        TicketId = ticketId;
        OldPriority = oldPriority;
        NewPriority = newPriority;
    }
}