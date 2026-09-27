using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserPhoneChangedEvent(
    UserId UserId,
    PhoneNumber OldPhone,
    PhoneNumber NewPhone) : DomainEvent;
