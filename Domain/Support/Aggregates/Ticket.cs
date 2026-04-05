using Domain.Support.Entities;
using Domain.Support.Enums;
using Domain.Support.Events;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Aggregates;

public sealed class Ticket : AggregateRoot<TicketId>, IAuditable
{
    private readonly List<TicketMessage> _messages = [];

    private Ticket()
    { }

    public UserId CustomerId { get; private set; } = default!;
    public UserId? AssignedAgentId { get; private set; }
    public string Subject { get; private set; } = default!;
    public TicketStatus Status { get; private set; } = default!;
    public TicketPriority Priority { get; private set; } = default!;
    public TicketCategory Category { get; private set; } = default!;
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<TicketMessage> Messages => _messages.AsReadOnly();

    public int MessageCount => _messages.Count;

    public bool IsClosed => Status.IsClosed;

    public bool IsOpen => Status == TicketStatus.Open;

    public bool IsAwaitingReply => Status == TicketStatus.AwaitingReply;

    public bool IsAnswered => Status == TicketStatus.Answered;

    public TicketMessage? LastMessage => _messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

    public static Ticket Open(
        TicketId id,
        UserId customerId,
        string subject,
        TicketCategory category,
        TicketPriority? priority = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(customerId, nameof(customerId));
        Guard.Against.NullOrWhiteSpace(subject, nameof(subject));
        Guard.Against.Null(category, nameof(category));

        var ticket = new Ticket
        {
            Id = id,
            CustomerId = customerId,
            Subject = subject.Trim(),
            Category = category,
            Priority = priority ?? TicketPriority.Normal,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

        ticket.RaiseDomainEvent(new TicketCreatedEvent(id, customerId, subject.Trim(), category.Value, priority ?? TicketPriority.Normal));
        return ticket;
    }

    public TicketMessage AddMessage(
        TicketMessageId messageId,
        UserId senderId,
        TicketMessageSenderType senderType,
        string content)
    {
        if (IsClosed)
            throw new DomainException("تیکت بسته شده است.");

        Guard.Against.NullOrWhiteSpace(content, nameof(content));

        var message = TicketMessage.Create(messageId, Id, senderId, senderType, content.Trim());
        _messages.Add(message);

        UpdateStatusAfterMessage(senderType);

        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketMessageAddedEvent(
            Id,
            messageId,
            CustomerId,
            senderId,
            senderType,
            _messages.Count));

        return message;
    }

    public void EditMessage(TicketMessageId messageId, UserId editorId, string newContent)
    {
        if (IsClosed)
            throw new DomainException("تیکت بسته شده است.");

        var message = _messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new DomainException("پیام یافت نشد.");

        if (message.SenderId != editorId)
            throw new DomainException("شما مجاز به ویرایش این پیام نیستید.");

        Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));

        message.EditContent(newContent.Trim());
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTo(UserId agentId)
    {
        if (IsClosed)
            throw new DomainException("تیکت بسته شده است.");

        Guard.Against.Null(agentId, nameof(agentId));

        var previousAgent = AssignedAgentId;
        AssignedAgentId = agentId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketAssignedEvent(Id, CustomerId, previousAgent, agentId));
    }

    public void Unassign()
    {
        if (AssignedAgentId is null) return;

        AssignedAgentId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePriority(TicketPriority newPriority)
    {
        Guard.Against.Null(newPriority, nameof(newPriority));

        if (Priority == newPriority) return;

        var previous = Priority;
        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketPriorityChangedEvent(Id, CustomerId, previous, newPriority));
    }

    public void UpdateSubject(string newSubject)
    {
        if (IsClosed)
            throw new DomainException("تیکت بسته شده است.");

        Guard.Against.NullOrWhiteSpace(newSubject, nameof(newSubject));

        Subject = newSubject.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketSubjectUpdatedEvent(Id, Subject));
    }

    public void Resolve()
    {
        if (IsClosed)
            throw new DomainException("تیکت بسته شده است.");

        var previous = Status;
        Status = TicketStatus.Answered;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Answered));
    }

    public void Close()
    {
        if (Status == TicketStatus.Closed) return;

        var previous = Status;
        Status = TicketStatus.Closed;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TicketClosedEvent(Id, CustomerId));
        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Closed));
    }

    public void Reopen(TicketPriority? newPriority = null)
    {
        if (!IsClosed) return;

        var previous = Status;
        Status = TicketStatus.Open;
        ResolvedAt = null;
        UpdatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;

        if (newPriority is not null)
            Priority = newPriority;

        RaiseDomainEvent(new TicketReopenedEvent(Id, CustomerId));
        RaiseDomainEvent(new TicketStatusChangedEvent(Id, CustomerId, previous, TicketStatus.Open));
    }

    private void UpdateStatusAfterMessage(TicketMessageSenderType senderType)
    {
        if (senderType == TicketMessageSenderType.Customer && IsAwaitingReply)
        {
            Status = TicketStatus.Open;
            return;
        }

        if (senderType == TicketMessageSenderType.Agent && IsOpen)
        {
            Status = TicketStatus.AwaitingReply;
            return;
        }

        if (senderType == TicketMessageSenderType.Agent && IsAnswered)
        {
            Status = TicketStatus.AwaitingReply;
        }
    }

    public bool IsHighPriority() => Priority == TicketPriority.High || Priority == TicketPriority.Urgent;

    public bool IsUrgent() => Priority == TicketPriority.Urgent;

    public bool RequiresUrgentAttention() => IsOpen && IsUrgent() && (DateTime.UtcNow - CreatedAt).TotalHours > 1;

    public TimeSpan? GetTimeToFirstResponse()
    {
        var customerMsg = _messages.FirstOrDefault(m => m.SenderType == TicketMessageSenderType.Customer);
        var agentMsg = _messages.FirstOrDefault(m => m.SenderType == TicketMessageSenderType.Agent);

        if (customerMsg != null && agentMsg != null && agentMsg.SentAt >= customerMsg.SentAt)
        {
            return agentMsg.SentAt - customerMsg.SentAt;
        }
        return null;
    }
}