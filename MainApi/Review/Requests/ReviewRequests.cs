namespace Presentation.Review.Requests;

public record CreateReviewRequest(
    Guid ProductId,
    int Rating,
    string? Title,
    string? Comment,
    Guid? OrderId = null
);

public record RejectReviewRequest(string? Reason = null);

public record AddAdminReplyRequest(string Reply);