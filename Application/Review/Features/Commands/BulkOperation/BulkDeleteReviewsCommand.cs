namespace Application.Review.Features.Commands.BulkOperation;

public sealed record BulkDeleteReviewsCommand(
    IReadOnlyList<Guid> ReviewIds,
    string? Reason = null)
    : ICommand<BulkOperationResult>, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewsBulkDeleted";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => string.Join(",", ReviewIds);
}
