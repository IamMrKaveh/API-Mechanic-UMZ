using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.User.Events;

public sealed class UserProfileUpdatedEvent(
    UserId userId,
    string firstName,
    string lastName,
    string? phoneNumber) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public string FirstName { get; } = firstName;
    public string LastName { get; } = lastName;
    public string? PhoneNumber { get; } = phoneNumber;
}