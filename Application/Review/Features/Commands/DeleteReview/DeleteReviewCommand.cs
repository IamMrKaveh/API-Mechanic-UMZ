namespace Application.Review.Features.Commands.DeleteReview;

public record DeleteReviewCommand(
    Guid ReviewId,
    string? Reason = null)
    : ICommand, IAuditableCommand, IHasConcurrencyMapping
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewDeleted";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();

    public string ConcurrencyMessage
        => "این نظر همزمان توسط مدیر دیگری تغییر داده شد. لطفاً صفحه را رفرش کنید و دوباره تلاش کنید.";
}
