using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserLoggedInEvent(UserId UserId) : DomainEvent;
