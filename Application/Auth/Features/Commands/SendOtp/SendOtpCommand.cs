using Domain.Security.Enums;

namespace Application.Auth.Features.Commands.SendOtp;

public record SendOtpCommand(
    string PhoneNumber,
    OtpPurpose Purpose = OtpPurpose.Login) : ICommand, IBypassTransactionBehavior, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "SendOtp";

    public string? AuditEntityType => "Otp";

    public string? AuditEntityId => PhoneNumber;
}