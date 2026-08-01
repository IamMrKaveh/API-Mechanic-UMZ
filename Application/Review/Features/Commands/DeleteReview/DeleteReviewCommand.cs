namespace Application.Review.Features.Commands.DeleteReview;

public record DeleteReviewCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewDeleted";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
