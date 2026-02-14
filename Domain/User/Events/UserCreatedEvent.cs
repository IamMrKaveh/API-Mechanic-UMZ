namespace Domain.User.Events;

public class UserCreatedEvent : DomainEvent
{
    public int UserId { get; }
    public string PhoneNumber { get; }

    public UserCreatedEvent(int userId, string phoneNumber)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
    }
}