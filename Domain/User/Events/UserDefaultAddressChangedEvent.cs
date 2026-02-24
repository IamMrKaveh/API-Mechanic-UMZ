namespace Domain.User.Events;

/// <summary>
/// رویداد تغییر آدرس پیش‌فرض کاربر
/// </summary>
public sealed class UserDefaultAddressChangedEvent : DomainEvent
{
    public int UserId { get; }
    public int NewDefaultAddressId { get; }

    public UserDefaultAddressChangedEvent(int userId, int newDefaultAddressId)
    {
        UserId = userId;
        NewDefaultAddressId = newDefaultAddressId;
    }
}