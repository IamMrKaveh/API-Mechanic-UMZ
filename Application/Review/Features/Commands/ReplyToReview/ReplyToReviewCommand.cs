namespace Application.Review.Features.Commands.ReplyToReview;

public record ReplyToReviewCommand(
    Guid ReviewId,
    string Reply)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewReplied";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
