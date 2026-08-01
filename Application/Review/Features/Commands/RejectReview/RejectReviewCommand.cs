namespace Application.Review.Features.Commands.RejectReview;

public record RejectReviewCommand(
    Guid ReviewId,
    string Reason)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewRejected";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
