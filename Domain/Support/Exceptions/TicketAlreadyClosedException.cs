namespace Domain.Support.Exceptions;

public sealed class TicketAlreadyClosedException(TicketId ticketId)
    : DomainException($"تیکت '{ticketId}' بسته شده است و قابل تغییر نیست.")
{
    public TicketId TicketId { get; } = ticketId;
}