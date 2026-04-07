using Domain.Common.Exceptions;
using Domain.Support.ValueObjects;

namespace Domain.Support.Exceptions;

public sealed class TicketNotFoundException : DomainException
{
    public TicketId TicketId { get; }

    public override string ErrorCode => "TICKET_NOT_FOUND";

    public TicketNotFoundException(TicketId ticketId)
        : base($"تیکت با شناسه '{ticketId}' یافت نشد.")
    {
        TicketId = ticketId;
    }
}