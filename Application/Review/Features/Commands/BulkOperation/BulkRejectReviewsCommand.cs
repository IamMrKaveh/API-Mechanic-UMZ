namespace Application.Review.Features.Commands.BulkOperation;

public sealed record BulkRejectReviewsCommand(
    IReadOnlyList<Guid> ReviewIds,
    string Reason)
    : ICommand<BulkOperationResult>, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewsBulkRejected";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => string.Join(",", ReviewIds);
}
