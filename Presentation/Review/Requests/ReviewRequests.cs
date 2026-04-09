namespace Presentation.Review.Requests;

public record SubmitReviewRequest(
    Guid ProductId,
    Guid? OrderId,
    int Rating,
    string? Title,
    string? Comment
);

public record RejectReviewRequest(string? Reason = null);

public record ReplyToReviewRequest(string Reply);

public record UpdateReviewStatusRequest(string Status);