using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.User.Events;

public sealed class UserDeletedEvent(
    UserId userId,
    UserId? deletedBy) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public UserId? DeletedBy { get; } = deletedBy;
}