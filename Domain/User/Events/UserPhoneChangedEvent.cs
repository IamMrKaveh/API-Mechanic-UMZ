namespace Domain.User.Events;

public class UserPhoneChangedEvent(int userId, string oldPhone, string newPhone) : DomainEvent
{
    public int UserId { get; } = userId;
    public string OldPhoneNumber { get; } = oldPhone;
    public string NewPhoneNumber { get; } = newPhone;
}