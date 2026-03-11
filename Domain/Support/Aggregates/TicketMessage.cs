using Domain.Support.Enums;

namespace Domain.Support.Aggregates;

public sealed class TicketMessage : AggregateRoot<TicketMessageId>
{
    private TicketMessage()
    { }

    public TicketId TicketId { get; private set; } = default!;
    public UserId SenderId { get; private set; } = default!;
    public TicketMessageSenderType SenderType { get; private set; }
    public string Content { get; private set; } = default!;
    public bool IsEdited { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public DateTime SentAt { get; private set; }

    public static TicketMessage Create(
        TicketMessageId id,
        TicketId ticketId,
        UserId senderId,
        TicketMessageSenderType senderType,
        string content)
    {
        var message = new TicketMessage
        {
            Id = id,
            TicketId = ticketId,
            SenderId = senderId,
            SenderType = senderType,
            Content = content,
            IsEdited = false,
            SentAt = DateTime.UtcNow
        };

        message.RaiseDomainEvent(new TicketMessageCreatedEvent(id, ticketId, senderId, senderType));
        return message;
    }

    public void EditContent(string newContent)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TicketMessageEditedEvent(Id, TicketId, SenderId));
    }
}