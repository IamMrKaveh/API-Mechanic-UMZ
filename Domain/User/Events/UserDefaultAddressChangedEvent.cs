using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed class UserDefaultAddressChangedEvent(
    UserId userId,
    UserAddressId? previousDefaultAddressId,
    UserAddressId newDefaultAddressId) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public UserAddressId? PreviousDefaultAddressId { get; } = previousDefaultAddressId;
    public UserAddressId NewDefaultAddressId { get; } = newDefaultAddressId;
}