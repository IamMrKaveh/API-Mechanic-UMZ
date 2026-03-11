using Domain.Common.Abstractions;
using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    string Email,
    string FirstName,
    string LastName) : IDomainEvent;