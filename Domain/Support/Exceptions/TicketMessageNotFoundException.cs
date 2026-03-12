namespace Domain.Support.Exceptions;

public sealed class TicketMessageNotFoundException(TicketMessageId messageId)
    : DomainException($"پیام تیکت با شناسه '{messageId}' یافت نشد.")
{
    public TicketMessageId MessageId { get; } = messageId;
}