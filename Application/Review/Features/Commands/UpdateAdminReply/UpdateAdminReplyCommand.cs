namespace Application.Review.Features.Commands.UpdateAdminReply;

public record UpdateAdminReplyCommand(
    Guid ReviewId,
    string Reply)
    : ICommand, IAuditableCommand, IHasConcurrencyMapping
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewReplyUpdated";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => ReviewId.ToString();

    public string ConcurrencyMessage
        => "این نظر همزمان توسط مدیر دیگری تغییر داده شد. لطفاً صفحه را رفرش کنید و دوباره تلاش کنید.";
}
