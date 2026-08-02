namespace Application.Review.Features.Commands.UpdateAdminReply;

public record UpdateAdminReplyCommand(
    Guid ReviewId,
    string Reply)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewAdminReplyUpdated";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
