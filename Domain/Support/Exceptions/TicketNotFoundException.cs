namespace Domain.Support.Exceptions;

public sealed class TicketNotFoundException : Common.Exceptions.DomainException
{
    public TicketNotFoundException(TicketId ticketId)
        : base($"تیکت با شناسه '{ticketId}' یافت نشد.")
    {
        TicketId = ticketId;
    }

    public TicketNotFoundException(int ticketId)
        : base($"تیکت با شناسه {ticketId} یافت نشد.")
    {
    }

    public TicketId? TicketId { get; }
}