using Domain.Common.Exceptions;
using Domain.Support.ValueObjects;

namespace Domain.Support.Exceptions;

public sealed class TicketMessageNotFoundException : DomainException
{
    public TicketMessageId MessageId { get; }

    public override string ErrorCode => "TICKET_MESSAGE_NOT_FOUND";

    public TicketMessageNotFoundException(TicketMessageId messageId)
        : base($"پیام تیکت با شناسه '{messageId}' یافت نشد.")
    {
        MessageId = messageId;
    }
}