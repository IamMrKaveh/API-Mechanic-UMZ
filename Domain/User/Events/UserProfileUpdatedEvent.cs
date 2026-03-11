using Domain.Common.Abstractions;
using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserProfileUpdatedEvent(
    UserId UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber) : IDomainEvent;