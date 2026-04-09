using Domain.Support.ValueObjects;

namespace Domain.Support.Exceptions;

public sealed class TicketAlreadyClosedException : DomainException
{
    public TicketId TicketId { get; }

    public override string ErrorCode => "TICKET_ALREADY_CLOSED";

    public TicketAlreadyClosedException(TicketId ticketId)
        : base($"تیکت '{ticketId}' بسته شده است و قابل تغییر نیست.")
    {
        TicketId = ticketId;
    }
}