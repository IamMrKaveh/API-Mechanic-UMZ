namespace Application.Review.Features.Commands.LikeReview;

public sealed record LikeReviewCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "UserEvent";

    public string AuditAction => "ReviewLiked";

    public string? AuditEntityType => "Review";

    public string? AuditEntityId => ReviewId.ToString();
}
