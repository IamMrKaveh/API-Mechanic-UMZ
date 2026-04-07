using Domain.User.ValueObjects;
using Domain.Common.Events;

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