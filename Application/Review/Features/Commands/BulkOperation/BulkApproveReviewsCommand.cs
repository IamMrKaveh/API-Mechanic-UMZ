namespace Application.Review.Features.Commands.BulkOperation;

public sealed record BulkApproveReviewsCommand(
    IReadOnlyList<Guid> ReviewIds)
    : ICommand<BulkOperationResult>, IAuditableCommand
{
    public string AuditEventType => "AdminEvent";
    public string AuditAction => "ReviewsBulkApproved";
    public string? AuditEntityType => "Review";
    public string? AuditEntityId => string.Join(",", ReviewIds);
}
