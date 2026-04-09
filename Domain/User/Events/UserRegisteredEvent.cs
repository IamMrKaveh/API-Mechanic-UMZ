using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed class UserRegisteredEvent(
    UserId userId,
    Email email,
    string firstName,
    string lastName) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public Email Email { get; } = email;
    public string FirstName { get; } = firstName;
    public string LastName { get; } = lastName;
}