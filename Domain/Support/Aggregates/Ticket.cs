using Domain.Support.Enums;

namespace Domain.Support.Aggregates;

public sealed class Ticket : AggregateRoot<TicketId>
{
    private Ticket()
    { }

    public UserId CustomerId { get; private set; } = default!;
    public UserId? AssignedAgentId { get; private set; }
    public string Subject { get; private set; } = default!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public string Category { get; private set; } = default!;
    public int MessageCount { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsClosed => Status is TicketStatus.Closed or TicketStatus.Resolved;

    public static Ticket Open(
        TicketId id,
        UserId customerId,
        string subject,
        string category,
        TicketPriority priority = TicketPriority.Normal)
    {
        var ticket = new Ticket
        {
            Id = id,
            CustomerId = customerId,
            Subject = subject,
            Category = category,
            Priority = priority,
            Status = TicketStatus.Open,
            MessageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        ticket.RaiseDomainEvent(new TicketCreatedEvent(id, customerId, subject, category, priority));
        return ticket;
    }

    public void RecordMessage(TicketMessageId messageId, UserId senderId, TicketMessageSenderType senderType)
    {
        if (IsClosed)
            throw new TicketAlreadyClosedException(Id);

        MessageCount++;
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (Status == TicketStatus.WaitingOnCustomer && senderType == TicketMessageSenderType.Customer)
            Status = TicketStatus.Open;

        if (Status == TicketStatus.Open && senderType == TicketMessageSenderType.Agent)
            Status = TicketStatus.WaitingOnCustomer;

        RaiseDomainEvent(new TicketMessageAddedEvent(Id, messageId, CustomerId, senderId, senderType, MessageCount));
    }

    public void AssignTo(UserId agentId)
    {
        if (IsClosed)
            throw new TicketAlreadyClosedException(Id);

        var previousAgent = AssignedAgentId;
        AssignedAgentId = agentId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TicketAssignedEvent(Id, CustomerId, previousAgent, agentId));
    }

    public void ChangePriority(TicketPriority priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        if (IsClosed)
            throw new TicketAlreadyClosedException(Id);

        var previous = Status;
        Status = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Resolved));
    }

    public void Close()
    {
        if (Status == TicketStatus.Closed)
            return;

        var previous = Status;
        Status = TicketStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TicketClosedEvent(Id, CustomerId));
        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Closed));
    }

    public void Reopen()
    {
        if (!IsClosed)
            return;

        var previous = Status;
        Status = TicketStatus.Open;
        ResolvedAt = null;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Open));
    }
}