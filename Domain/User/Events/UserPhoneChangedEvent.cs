using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed class UserPhoneChangedEvent(
    UserId userId,
    PhoneNumber oldPhone,
    PhoneNumber newPhone) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public PhoneNumber OldPhone { get; } = oldPhone;
    public PhoneNumber NewPhone { get; } = newPhone;
}