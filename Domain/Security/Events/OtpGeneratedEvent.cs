using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record OtpGeneratedEvent(
    OtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose,
    DateTime ExpiresAt) : DomainEvent;
