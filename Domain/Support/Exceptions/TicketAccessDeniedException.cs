using Domain.Common.Exceptions;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Exceptions;

public sealed class TicketAccessDeniedException : DomainException
{
    public TicketId TicketId { get; }
    public UserId UserId { get; }

    public override string ErrorCode => "TICKET_ACCESS_DENIED";

    public TicketAccessDeniedException(TicketId ticketId, UserId userId)
        : base($"کاربر '{userId}' دسترسی به تیکت '{ticketId}' را ندارد.")
    {
        TicketId = ticketId;
        UserId = userId;
    }
}