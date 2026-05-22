using Domain.Support.Aggregates;
using Domain.Support.Results;
using Domain.User.ValueObjects;

namespace Domain.Support.Services;

public sealed class TicketDomainService
{
    public static TicketAccessResult ValidateUserAccess(
        Ticket ticketId,
        UserId userId,
        bool isAdmin)
    {
        Guard.Against.Null(ticketId, nameof(ticketId));
        Guard.Against.Null(userId, nameof(userId));

        if (isAdmin)
            return TicketAccessResult.Allowed();

        if (ticketId.CustomerId != userId && ticketId.AssignedAgentId != userId)
            return TicketAccessResult.Denied("شما دسترسی به این تیکت را ندارید.");

        return TicketAccessResult.Allowed();
    }
}