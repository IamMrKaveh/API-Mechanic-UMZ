namespace Application.Review.Features.Commands.UpdateReviewStatus;

public record UpdateReviewStatusCommand(
    Guid ReviewId,
    string Status,
    string? Reason = null)
    : ICommand, IAuditableCommand, IHasConcurrencyMapping
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewStatusUpdated";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();

    public string ConcurrencyMessage
        => "این نظر همزمان توسط مدیر دیگری تغییر داده شد. لطفاً صفحه را رفرش کنید و دوباره تلاش کنید.";
}
