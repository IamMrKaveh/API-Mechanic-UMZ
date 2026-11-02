namespace Application.User.Features.Commands.RequestPhoneChangeOtp;

public record RequestPhoneChangeOtpCommand(string NewPhoneNumber)
    : ICommand, IBypassTransactionBehavior, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "RequestPhoneChangeOtp";

    public string? AuditEntityType => "Otp";

    public string? AuditEntityId => NewPhoneNumber;
}
