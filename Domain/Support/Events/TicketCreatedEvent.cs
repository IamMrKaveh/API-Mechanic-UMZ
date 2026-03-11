using Domain.Common.Abstractions;
using Domain.Support.Enums;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Support.Events;

public sealed record TicketCreatedEvent(
    TicketId TicketId,
    UserId CustomerId,
    string Subject,
    string Category,
    TicketPriority Priority) : IDomainEvent;