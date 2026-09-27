using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserAddressUpdatedEvent(
    UserId UserId,
    UserAddressId AddressId) : DomainEvent;
