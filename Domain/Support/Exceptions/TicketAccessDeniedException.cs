namespace Domain.Support.Exceptions;

public sealed class TicketAccessDeniedException(int ticketId, int userId) : DomainException($"کاربر {userId} دسترسی به تیکت {ticketId} را ندارد.")
{
    public int TicketId { get; } = ticketId;
    public int UserId { get; } = userId;
}