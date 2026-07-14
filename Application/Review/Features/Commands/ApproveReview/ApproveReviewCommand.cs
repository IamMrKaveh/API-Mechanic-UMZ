namespace Application.Review.Features.Commands.ApproveReview;

public record ApproveReviewCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";

    public string AuditAction => "ReviewApproved";

    public string? AuditEntityType => "Review";

    public string? AuditEntityId => ReviewId.ToString();
}