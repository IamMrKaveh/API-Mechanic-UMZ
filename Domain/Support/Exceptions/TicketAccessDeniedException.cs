namespace Domain.Support.Exceptions;

public sealed class TicketAccessDeniedException : Common.Exceptions.DomainException
{
    public TicketAccessDeniedException(TicketId ticketId, UserId userId)
        : base($"کاربر '{userId}' دسترسی به تیکت '{ticketId}' را ندارد.")
    {
        TicketId = ticketId;
        UserId = userId;
    }

    public TicketAccessDeniedException(int ticketId, int userId)
        : base($"کاربر {userId} دسترسی به تیکت {ticketId} را ندارد.")
    {
    }

    public TicketId? TicketId { get; }
    public UserId? UserId { get; }
}