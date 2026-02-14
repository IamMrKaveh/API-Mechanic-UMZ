namespace Domain.User.Events;

public class UserPhoneChangedEvent : DomainEvent
{
    public int UserId { get; }
    public string OldPhoneNumber { get; }
    public string NewPhoneNumber { get; }

    public UserPhoneChangedEvent(int userId, string oldPhone, string newPhone)
    {
        UserId = userId;
        OldPhoneNumber = oldPhone;
        NewPhoneNumber = newPhone;
    }
}