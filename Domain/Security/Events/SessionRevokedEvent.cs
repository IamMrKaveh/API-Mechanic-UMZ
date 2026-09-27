using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record SessionRevokedEvent(
    SessionId SessionId,
    UserId UserId,
    SessionRevocationReason Reason) : DomainEvent;
