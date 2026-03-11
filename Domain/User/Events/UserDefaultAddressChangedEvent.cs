using Domain.Common.Abstractions;
using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserDefaultAddressChangedEvent(
    UserId UserId,
    UserAddressId? PreviousDefaultAddressId,
    UserAddressId NewDefaultAddressId) : IDomainEvent;