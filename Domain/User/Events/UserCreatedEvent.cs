namespace Domain.User.Events;

public class UserCreatedEvent(UserId userId, string phoneNumber) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public string PhoneNumber { get; } = phoneNumber;
}