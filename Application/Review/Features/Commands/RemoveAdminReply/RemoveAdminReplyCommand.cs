namespace Application.Review.Features.Commands.RemoveAdminReply;

public record RemoveAdminReplyCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewAdminReplyRemoved";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
