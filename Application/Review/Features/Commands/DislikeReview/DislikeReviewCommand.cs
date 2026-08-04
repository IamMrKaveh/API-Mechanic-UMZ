namespace Application.Review.Features.Commands.DislikeReview;

public sealed record DislikeReviewCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "UserEvent";

    public string AuditAction => "ReviewDisliked";

    public string? AuditEntityType => "Review";

    public string? AuditEntityId => ReviewId.ToString();
}
