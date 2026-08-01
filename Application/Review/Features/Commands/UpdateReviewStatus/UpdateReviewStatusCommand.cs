namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(
    Guid ReviewId,
    string Status,
    string? Reason = null)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewStatusUpdated";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
