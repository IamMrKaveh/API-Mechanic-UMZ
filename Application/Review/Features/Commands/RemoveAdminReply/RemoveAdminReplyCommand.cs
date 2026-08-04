namespace Application.Review.Features.Commands.RemoveAdminReply;

public record RemoveAdminReplyCommand(
    Guid ReviewId)
    : ICommand, IAuditableCommand, IHasConcurrencyMapping
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewReplyRemoved";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();

    public string ConcurrencyMessage
        => "این نظر همزمان توسط مدیر دیگری تغییر داده شد. لطفاً صفحه را رفرش کنید و دوباره تلاش کنید.";
}
