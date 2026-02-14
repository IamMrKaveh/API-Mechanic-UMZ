namespace Domain.Support.Exceptions;

public sealed class TicketAccessDeniedException : DomainException
{
    public int TicketId { get; }
    public int UserId { get; }

    public TicketAccessDeniedException(int ticketId, int userId)
        : base($"کاربر {userId} دسترسی به تیکت {ticketId} را ندارد.")
    {
        TicketId = ticketId;
        UserId = userId;
    }
}