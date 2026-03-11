namespace Domain.Support.Exceptions;

public sealed class TicketNotFoundException(int ticketId) : DomainException($"تیکت با شناسه {ticketId} یافت نشد.")
{
    public int TicketId { get; } = ticketId;
}