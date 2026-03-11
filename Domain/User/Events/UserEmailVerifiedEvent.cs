using Domain.Common.Abstractions;
using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserEmailVerifiedEvent(UserId UserId, string Email) : IDomainEvent;