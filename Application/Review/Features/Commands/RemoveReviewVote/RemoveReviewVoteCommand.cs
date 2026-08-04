namespace Application.Review.Features.Commands.RemoveReviewVote;

public sealed record RemoveReviewVoteCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "UserEvent";

    public string AuditAction => "ReviewVoteRemoved";

    public string? AuditEntityType => "Review";

    public string? AuditEntityId => ReviewId.ToString();
}
