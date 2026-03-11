namespace Domain.User.Events;

public class OtpGeneratedEvent(int userId, string phoneNumber) : DomainEvent
{
    public int UserId { get; } = userId;
    public string PhoneNumber { get; } = phoneNumber;
}