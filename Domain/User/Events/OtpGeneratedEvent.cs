namespace Domain.User.Events;

public class OtpGeneratedEvent : DomainEvent
{
    public int UserId { get; }
    public string PhoneNumber { get; }

    public OtpGeneratedEvent(int userId, string phoneNumber)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
    }
}