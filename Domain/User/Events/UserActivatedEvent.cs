using Domain.Common.Abstractions;
using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserActivatedEvent(UserId UserId) : IDomainEvent;