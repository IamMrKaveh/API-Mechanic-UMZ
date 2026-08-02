namespace Presentation.Review.Requests;

public record ReplyToReviewRequest(string Reply);

public record RejectReviewRequest(string Reason);

public record UpdateReviewStatusRequest(string Status, string? Reason = null);

public record BulkReviewActionRequest(IReadOnlyList<Guid> ReviewIds);

public record BulkRejectReviewsRequest(IReadOnlyList<Guid> ReviewIds, string Reason);

public record BulkDeleteReviewsRequest(IReadOnlyList<Guid> ReviewIds, string? Reason = null);
