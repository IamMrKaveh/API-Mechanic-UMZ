using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Entities;

public sealed class TicketMessage : Entity<TicketMessageId>
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

    internal static TicketMessage Create(
        TicketMessageId id,
        TicketId ticketId,
        UserId senderId,
        TicketMessageSenderType senderType,
        string content,
        DateTime now)
    {
        return new TicketMessage
        {
            Id = id,
            TicketId = ticketId,
            SenderId = senderId,
            SenderType = senderType,
            Content = content,
            IsEdited = false,
            SentAt = now
        };
    }

    internal void EditContent(string newContent, DateTime now)
    {
        Content = newContent;
        IsEdited = true;
        EditedAt = now;
    }

    public bool IsFromCustomer() => SenderType == TicketMessageSenderType.Customer;

    public bool IsFromAgent() => SenderType == TicketMessageSenderType.Agent;

    public bool IsFromSystem() => SenderType == TicketMessageSenderType.System;

    public bool WasEditedAfter(TimeSpan threshold) =>
        IsEdited && EditedAt.HasValue && EditedAt.Value - SentAt > threshold;
}