using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Support.Events;

public sealed class TicketAnsweredEvent(
    TicketId ticketId,
    UserId adminId) : DomainEvent
{
    public TicketId TicketId { get; } = ticketId;
    public UserId AdminId { get; } = adminId;
}