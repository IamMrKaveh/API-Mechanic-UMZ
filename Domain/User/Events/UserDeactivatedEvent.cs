using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserDeactivatedEvent(UserId UserId) : DomainEvent;
