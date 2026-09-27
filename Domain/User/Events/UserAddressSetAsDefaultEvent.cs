using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserAddressSetAsDefaultEvent(
    UserId UserId,
    UserAddressId AddressId) : DomainEvent;
