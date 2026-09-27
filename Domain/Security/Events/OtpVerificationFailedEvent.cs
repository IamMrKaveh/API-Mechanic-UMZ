using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record OtpVerificationFailedEvent(
    OtpId OtpId,
    UserId UserId,
    OtpPurpose Purpose,
    int AttemptNumber,
    int RemainingAttempts) : DomainEvent;
