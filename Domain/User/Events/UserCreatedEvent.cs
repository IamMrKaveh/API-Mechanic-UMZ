namespace Domain.User.Events;

public class UserCreatedEvent(int userId, string phoneNumber) : DomainEvent
{
    public int UserId { get; } = userId;
    public string PhoneNumber { get; } = phoneNumber;
}