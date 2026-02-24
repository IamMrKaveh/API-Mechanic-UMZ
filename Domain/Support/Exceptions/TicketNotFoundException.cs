namespace Domain.Support.Exceptions;

public sealed class TicketNotFoundException : DomainException
{
    public int TicketId { get; }

    public TicketNotFoundException(int ticketId)
        : base($"تیکت با شناسه {ticketId} یافت نشد.")
    {
        TicketId = ticketId;
    }
}