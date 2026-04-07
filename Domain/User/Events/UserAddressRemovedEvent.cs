using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.User.Events;

public sealed class UserAddressRemovedEvent(
    UserId userId,
    UserAddressId addressId) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public UserAddressId AddressId { get; } = addressId;
}