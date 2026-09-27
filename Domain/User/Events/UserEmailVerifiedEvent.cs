using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserEmailVerifiedEvent(UserId UserId, Email Email) : DomainEvent;
