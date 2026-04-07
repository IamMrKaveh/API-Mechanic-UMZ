using Domain.User.ValueObjects;

namespace Domain.User.Events;

public class UserCreatedEvent(UserId userId, PhoneNumber phoneNumber) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public PhoneNumber PhoneNumber { get; } = phoneNumber;
}