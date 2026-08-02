namespace Application.Review.Features.Commands.RestoreReview;

public record RestoreReviewCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewRestored";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();
}
