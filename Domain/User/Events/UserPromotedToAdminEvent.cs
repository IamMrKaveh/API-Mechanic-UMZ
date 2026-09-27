using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserPromotedToAdminEvent(UserId UserId) : DomainEvent;
