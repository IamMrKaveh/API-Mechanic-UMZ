using Domain.Support.ValueObjects;

namespace Domain.Support.Exceptions;

public sealed class TicketAlreadyClosedException(TicketId ticketId) : Exception($"Ticket '{ticketId}' is already closed and cannot be modified.")
{
}