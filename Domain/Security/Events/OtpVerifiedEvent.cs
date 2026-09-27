using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record OtpVerifiedEvent(
    OtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose) : DomainEvent;
