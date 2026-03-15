using Domain.User.ValueObjects;

namespace Domain.User.Events;

public class OtpGeneratedEvent(UserId userId, string phoneNumber) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public string PhoneNumber { get; } = phoneNumber;
}