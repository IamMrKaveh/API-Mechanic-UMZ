using Domain.User.ValueObjects;

namespace Domain.User.Events;

public class UserPhoneChangedEvent(UserId userId, string oldPhone, string newPhone) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public string OldPhoneNumber { get; } = oldPhone;
    public string NewPhoneNumber { get; } = newPhone;
}