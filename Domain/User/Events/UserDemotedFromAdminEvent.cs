using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserDemotedFromAdminEvent(UserId UserId) : DomainEvent;
